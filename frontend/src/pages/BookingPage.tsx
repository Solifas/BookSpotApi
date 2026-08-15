import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { apiClient, createIdempotencyKey } from '../services/api';
import { useAuth } from '../contexts/AuthContext';
import { bookingErrorMessage } from '../lib/bookingErrors';
import { businessDate, businessDayRange, businessTimeLabel } from '../lib/businessTime';
import type { AvailabilitySlotDto, BusinessDto, ServiceDto } from '../types/api';

const BookingPage = () => {
  const { businessId } = useParams<{ businessId: string }>();
  const { user } = useAuth();
  const navigate = useNavigate();
  const [business, setBusiness] = useState<BusinessDto | null>(null);
  const [services, setServices] = useState<ServiceDto[]>([]);
  const [serviceId, setServiceId] = useState('');
  const [date, setDate] = useState('');
  const [slots, setSlots] = useState<AvailabilitySlotDto[]>([]);
  const [startTime, setStartTime] = useState('');
  const [loading, setLoading] = useState(true);
  const [loadingSlots, setLoadingSlots] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const bookingOperationKey = useRef<string | null>(null);

  useEffect(() => {
    if (!businessId) { setError('A business identifier is required.'); setLoading(false); return; }
    void Promise.all([apiClient.getBusinessDetails(businessId), apiClient.getBusinessServiceCatalog(businessId)]).then(([businessResponse, servicesResponse]) => {
      setLoading(false);
      if (businessResponse.error || servicesResponse.error) {
        setError(businessResponse.error || servicesResponse.error || 'Booking information is unavailable.');
        return;
      }
      setBusiness(businessResponse.data ?? null);
      setServices(servicesResponse.data ?? []);
    });
  }, [businessId]);

  const loadAvailability = useCallback(async (preserveError = false) => {
    if (!serviceId || !date || !business) { setSlots([]); return; }
    setLoadingSlots(true); setStartTime('');
    if (!preserveError) setError('');
    const range = businessDayRange(date, business.timeZone);
    const response = await apiClient.getServiceAvailability(serviceId, range.from, range.to);
    setLoadingSlots(false);
    if (response.error) { setError(response.error); setSlots([]); return; }
    setSlots(response.data?.slots ?? []);
  }, [business, date, serviceId]);

  useEffect(() => { void loadAvailability(); }, [loadAvailability]);

  const submit = async () => {
    if (user?.type !== 'client') { setError('Your account is not allowed to make this booking.'); return; }
    if (!serviceId || !startTime) { setError('Choose a service and an available time.'); return; }
    setSubmitting(true); setError('');
    bookingOperationKey.current ??= createIdempotencyKey();
    const response = await apiClient.createBooking({ serviceId, startTime }, bookingOperationKey.current);
    setSubmitting(false);
    if (response.error) {
      setError(bookingErrorMessage(response));
      if (response.status !== 0 && response.status !== 503) bookingOperationKey.current = null;
      if (response.status === 409) await loadAvailability(true);
      return;
    }
    bookingOperationKey.current = null;
    navigate('/dashboard');
  };

  return (
    <div className="min-h-screen bg-slate-50">
      <main className="max-w-3xl mx-auto px-4 sm:px-6 py-8">
        <Link to="/" className="text-blue-700">← Back to services</Link>
        <h1 className="text-3xl font-bold mt-5">Book {business?.businessName || 'a service'}</h1>
        {loading && <p role="status" className="mt-6">Loading services…</p>}
        {error && <div role="alert" className="mt-6 bg-red-50 border border-red-200 text-red-800 p-4 rounded-xl">{error}</div>}
        {!loading && user?.type === 'provider' && <div role="alert" className="mt-6 bg-amber-50 border border-amber-200 p-4 rounded-xl">Provider accounts cannot create bookings. Use a client account.</div>}
        {!loading && user?.type === 'client' && (
          <section className="mt-6 bg-white border rounded-2xl p-5 sm:p-8 space-y-6">
            <div><label htmlFor="service" className="block font-medium mb-2">Service</label><select id="service" value={serviceId} onChange={(e) => { bookingOperationKey.current = null; setServiceId(e.target.value); }} className="w-full p-3 border rounded-xl"><option value="">Choose a service</option>{services.map((service) => <option key={service.serviceId} value={service.serviceId}>{service.name} — R{service.price.amount.toFixed(2)}</option>)}</select></div>
            <div><label htmlFor="date" className="block font-medium mb-2">Date ({business?.timeZone})</label><input id="date" type="date" min={business ? businessDate(business.timeZone) : undefined} value={date} onChange={(e) => { bookingOperationKey.current = null; setDate(e.target.value); }} className="w-full p-3 border rounded-xl" /></div>
            <fieldset><legend className="font-medium mb-2">Available times</legend>
              {loadingSlots ? <p role="status">Loading availability…</p> : slots.length ? <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">{slots.map((slot) => <button key={slot.startTime} type="button" aria-pressed={startTime === slot.startTime} onClick={() => { bookingOperationKey.current = null; setStartTime(slot.startTime); }} className={`p-3 border rounded-xl ${startTime === slot.startTime ? 'bg-blue-700 text-white' : 'bg-white'}`}>{businessTimeLabel(slot.startTime, business.timeZone)}</button>)}</div> : serviceId && date ? <p className="text-slate-600">No available times on this date.</p> : <p className="text-slate-600">Choose a service and date to see live availability.</p>}
            </fieldset>
            <button type="button" onClick={() => void submit()} disabled={submitting || !startTime} className="w-full bg-blue-700 text-white py-3 rounded-xl disabled:opacity-60">{submitting ? 'Booking…' : 'Confirm booking'}</button>
          </section>
        )}
      </main>
    </div>
  );
};

export default BookingPage;
