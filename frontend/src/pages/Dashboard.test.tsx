import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import Dashboard from './Dashboard';
import { apiClient } from '../services/api';
import type { ProviderDashboardDto } from '../types/api';

vi.mock('../contexts/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'provider-1', name: 'Provider', email: 'p@example.com', type: 'provider', contactNumber: null } }),
}));
vi.mock('../components/Navigation', () => ({ default: () => <nav>Navigation</nav> }));

const dashboard: ProviderDashboardDto = {
  kind: 'provider', generatedAt: '2026-08-14T10:00:00Z', timeZone: 'Africa/Johannesburg',
  todayBookings: 2, weekBookings: 6, pendingRequests: 1, totalClients: 4, activeServices: 3,
  monthlyRevenue: { amount: 1250, currency: 'ZAR' }, recentClients: [],
  upcoming: [{
    bookingId: 'booking-1', serviceId: 'service-1', businessId: 'business-1', providerProfileId: 'provider-1',
    status: 'pending', startTime: '2026-08-15T10:00:00Z', endTime: '2026-08-15T11:00:00Z',
    price: { amount: 120, currency: 'ZAR' }, version: 3, createdAt: '2026-08-14T00:00:00Z', updatedAt: '2026-08-14T00:00:00Z',
    service: { name: 'Haircut', durationMinutes: 60 }, business: { businessName: 'Shop', address: '1 Road', city: 'Joburg' },
    view: 'provider', client: { fullName: 'Client', email: 'c@example.com', contactNumber: null },
  }],
};

describe('Dashboard', () => {
  beforeEach(() => {
    vi.spyOn(apiClient, 'getDashboard').mockResolvedValue({ status: 200, data: dashboard });
  });

  it('renders persisted dashboard data from /dashboard/me', async () => {
    render(<Dashboard />);
    expect(await screen.findByText(/1,250\.00/)).toBeInTheDocument();
    expect(screen.getByText('Haircut')).toBeInTheDocument();
    expect(screen.getByText('1 pending')).toBeInTheDocument();
  });

  it('reuses an immutable action request and key after an ambiguous 503', async () => {
    vi.mocked(apiClient.getDashboard)
      .mockResolvedValueOnce({ status: 200, data: dashboard })
      .mockResolvedValue({ status: 200, data: { ...dashboard, upcoming: [{ ...dashboard.upcoming[0], version: 4 }] } });
    const action = vi.spyOn(apiClient, 'performBookingAction')
      .mockResolvedValueOnce({ status: 503, code: 'persistence_unavailable', error: 'Unavailable' })
      .mockResolvedValueOnce({ status: 200, data: { view: 'provider', bookingId: 'booking-1', status: 'confirmed', version: 4, startTime: dashboard.upcoming[0].startTime, endTime: dashboard.upcoming[0].endTime, updatedAt: '2026-08-14T10:01:00Z' } });
    render(<Dashboard />);
    fireEvent.click(await screen.findByRole('button', { name: 'Confirm Haircut booking' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('temporarily unavailable');
    fireEvent.click(await screen.findByRole('button', { name: 'Confirm Haircut booking' }));
    await waitFor(() => expect(action).toHaveBeenCalledTimes(2));
    expect(action.mock.calls[0][1]).toEqual({ action: 'confirm', expectedVersion: 3 });
    expect(action.mock.calls[1][1]).toEqual(action.mock.calls[0][1]);
    expect(action.mock.calls[1][2]).toBe(action.mock.calls[0][2]);
  });

  it('shows a recoverable concurrency message and reloads after a booking conflict', async () => {
    vi.spyOn(apiClient, 'performBookingAction').mockResolvedValue({ status: 409, code: 'booking_state_conflict', error: 'Booking state changed.' });
    render(<Dashboard />);
    fireEvent.click(await screen.findByRole('button', { name: 'Confirm Haircut booking' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('changed since it was loaded');
    await waitFor(() => expect(apiClient.getDashboard).toHaveBeenCalledTimes(2));
  });
});
