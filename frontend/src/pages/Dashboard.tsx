import { useCallback, useEffect, useRef, useState } from 'react';
import { Calendar, Clock, RefreshCw, TrendingUp, Users, type LucideIcon } from 'lucide-react';
import Navigation from '../components/Navigation';
import { useAuth } from '../contexts/AuthContext';
import { apiClient, createIdempotencyKey, type ApiResponse } from '../services/api';
import { businessDateTimeLabel } from '../lib/businessTime';
import type { BookingAction, BookingDto, BookingMutationResultDto, DashboardDto } from '../types/api';

const money = (amount: number) => `R\u00a0${amount.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;

const actionError = (response: ApiResponse<BookingMutationResultDto>) => {
  if (response.status === 401) return 'Your session has expired. Sign in again before retrying.';
  if (response.status === 403) return 'You are not authorized to perform that booking action.';
  if (response.status === 409 && response.code === 'booking_slot_conflict') return 'That time slot was just reserved. Choose another available time.';
  if (response.status === 409) return 'This booking changed since it was loaded. The latest details are shown; review them before retrying.';
  if (response.status === 400) return response.error || 'The booking action is not valid in its current state.';
  if (response.status === 503) return 'The service is temporarily unavailable. Retry this same action in a moment.';
  return response.error || 'The booking could not be updated.';
};

const Dashboard = () => {
  const { user } = useAuth();
  const [dashboard, setDashboard] = useState<DashboardDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [actionMessage, setActionMessage] = useState('');
  const [busyBooking, setBusyBooking] = useState<string | null>(null);
  const pendingOperations = useRef(new Map<string, { key: string; request: { action: BookingAction; expectedVersion: number } }>());

  const load = useCallback(async () => {
    setLoading(true);
    const response = await apiClient.getDashboard();
    setLoading(false);
    if (response.error || !response.data) {
      setError(response.error || 'Dashboard data is unavailable.');
      return;
    }
    setError('');
    setDashboard(response.data);
  }, []);

  useEffect(() => { void load(); }, [load]);

  const perform = async (booking: BookingDto, action: BookingAction) => {
    setBusyBooking(booking.bookingId);
    setActionMessage('');
    const operation = `${booking.bookingId}:${action}`;
    const pending = pendingOperations.current.get(operation) ?? {
      key: createIdempotencyKey(), request: { action, expectedVersion: booking.version },
    };
    pendingOperations.current.set(operation, pending);
    const response = await apiClient.performBookingAction(booking.bookingId, pending.request, pending.key);
    setBusyBooking(null);
    if (response.error) {
      if (response.status !== 0 && response.status !== 503) pendingOperations.current.delete(operation);
      setActionMessage(actionError(response));
    } else {
      pendingOperations.current.delete(operation);
    }
    await load();
  };

  const stats: Array<[string, string | number, LucideIcon]> = dashboard?.kind === 'provider'
    ? [
      ['Today', dashboard.todayBookings, Calendar], ['This week', dashboard.weekBookings, Clock],
      ['Clients', dashboard.totalClients, Users], ['Monthly revenue', money(dashboard.monthlyRevenue.amount), TrendingUp],
    ]
    : dashboard ? [
      ['Bookings', dashboard.totalBookings, Calendar], ['Completed', dashboard.completedBookings, Clock],
      ['Pending', dashboard.pendingRequests, Users], ['Total spent', money(dashboard.totalSpent.amount), TrendingUp],
    ] : [];

  return (
    <div className="min-h-screen bg-slate-50">
      <Navigation />
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-8">
          <div><h1 className="text-3xl font-bold text-slate-900">Welcome, {user?.name}</h1><p className="text-slate-600">Your live booking overview</p></div>
          <button onClick={() => void load()} disabled={loading} className="inline-flex items-center justify-center gap-2 border px-4 py-2 rounded-xl bg-white disabled:opacity-60"><RefreshCw className="h-4 w-4" /> Refresh</button>
        </div>
        {loading && <p role="status">Loading dashboard…</p>}
        {error && <div role="alert" className="bg-red-50 border border-red-200 text-red-800 p-4 rounded-xl">{error}</div>}
        {actionMessage && <div role="alert" className="bg-amber-50 border border-amber-200 text-amber-900 p-4 rounded-xl mb-6">{actionMessage}</div>}
        {dashboard && (
          <>
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
              {stats.map(([label, value, Icon]) => <section key={String(label)} className="bg-white border rounded-2xl p-5 shadow-sm"><Icon className="h-5 w-5 text-blue-600" /><p className="text-sm text-slate-600 mt-3">{String(label)}</p><p className="text-2xl font-bold">{String(value)}</p></section>)}
            </div>
            <section className="bg-white border rounded-2xl p-5 sm:p-6 shadow-sm">
              <div className="flex items-center justify-between"><h2 className="text-xl font-bold">Upcoming bookings</h2><span className="text-sm text-slate-600">{dashboard.kind === 'provider' ? `${dashboard.pendingRequests} pending` : `${dashboard.upcoming.length} upcoming`}</span></div>
              {dashboard.upcoming.length === 0 ? <p className="py-10 text-center text-slate-600">No upcoming bookings.</p> : (
                <ul className="divide-y mt-4">
                  {dashboard.upcoming.map((booking) => (
                    <li key={booking.bookingId} className="py-5 flex flex-col md:flex-row md:items-center md:justify-between gap-4">
                      <div><p className="font-semibold">{booking.service.name}</p><p className="text-sm text-slate-600">{dashboard.kind === 'provider' ? businessDateTimeLabel(booking.startTime, dashboard.timeZone) : new Date(booking.startTime).toLocaleString()} · {booking.business.businessName}</p><p className="text-sm capitalize mt-1">{booking.status}</p></div>
                      <div className="flex flex-wrap gap-2">
                        {dashboard.kind === 'provider' && booking.status === 'pending' && <>
                          <button aria-label={`Confirm ${booking.service.name} booking`} disabled={busyBooking === booking.bookingId} onClick={() => void perform(booking, 'confirm')} className="bg-green-700 text-white px-4 py-2 rounded-lg disabled:opacity-60">Confirm</button>
                          <button aria-label={`Decline ${booking.service.name} booking`} disabled={busyBooking === booking.bookingId} onClick={() => void perform(booking, 'decline')} className="bg-red-700 text-white px-4 py-2 rounded-lg disabled:opacity-60">Decline</button>
                        </>}
                        {dashboard.kind === 'client' && ['pending', 'confirmed'].includes(booking.status) && <button aria-label={`Cancel ${booking.service.name} booking`} disabled={busyBooking === booking.bookingId} onClick={() => void perform(booking, 'cancel')} className="bg-red-700 text-white px-4 py-2 rounded-lg disabled:opacity-60">Cancel</button>}
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </section>
          </>
        )}
      </main>
    </div>
  );
};

export default Dashboard;
