import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import Settings from './Settings';
import { apiClient } from '../services/api';
import type { BusinessDto } from '../types/api';

const refreshProfile = vi.fn();
vi.mock('../contexts/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'provider-1', name: 'Provider', email: 'p@example.com', type: 'provider', contactNumber: null }, refreshProfile }),
}));
vi.mock('../components/Navigation', () => ({ default: () => <nav>Navigation</nav> }));

const business: BusinessDto = {
  businessId: 'business-1', providerProfileId: 'provider-1', businessName: 'Shop', description: 'A good shop',
  address: '1 Main Road', city: 'Johannesburg', phone: '+27 82 123 4567', email: 'shop@example.com',
  website: null, imageUrl: null, isActive: true, rating: 4, reviewCount: 2, timeZone: 'Africa/Johannesburg', createdAt: '2026-08-14T00:00:00Z',
};

describe('Settings', () => {
  beforeEach(() => {
    vi.spyOn(apiClient, 'getMyBusinesses').mockResolvedValue({ status: 200, data: [business] });
  });

  it('lets providers select and persist a second owned business', async () => {
    const second = { ...business, businessId: 'business-2', businessName: 'Second Shop', city: 'Cape Town', email: 'second@example.com' };
    vi.mocked(apiClient.getMyBusinesses).mockResolvedValue({ status: 200, data: [business, second] });
    const update = vi.spyOn(apiClient, 'updateBusiness').mockResolvedValue({ status: 200, data: { ...second, city: 'Durban' } });
    render(<Settings />);
    fireEvent.change(await screen.findByLabelText('Business to edit'), { target: { value: 'business-2' } });
    const city = await screen.findByLabelText(/^City/);
    await waitFor(() => expect(city).toHaveValue('Cape Town'));
    fireEvent.change(city, { target: { value: 'Durban' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save business settings' }));
    await waitFor(() => expect(update).toHaveBeenCalledWith('business-2', expect.objectContaining({ city: 'Durban' })));
  });

  it('loads and persists provider settings using the business identifier', async () => {
    const update = vi.spyOn(apiClient, 'updateBusiness').mockResolvedValue({ status: 200, data: { ...business, city: 'Pretoria' } });
    render(<Settings />);
    const city = await screen.findByLabelText(/^City/);
    expect(city).toHaveValue('Johannesburg');
    fireEvent.change(city, { target: { value: 'Pretoria' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save business settings' }));
    await waitFor(() => expect(update).toHaveBeenCalledWith('business-1', expect.objectContaining({ city: 'Pretoria' })));
    expect(await screen.findByRole('status')).toHaveTextContent('Settings saved');
  });
});
