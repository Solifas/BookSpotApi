import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiClient, createIdempotencyKey } from './api';

const jsonResponse = (body: unknown, status = 200, contentType = 'application/json') =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } });

describe('ApiClient contract', () => {
  let client: ApiClient;

  beforeEach(() => {
    client = new ApiClient('http://localhost:5000');
  });

  it('preserves ProblemDetails status, code, and field errors', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse({
      title: 'Validation failed',
      detail: 'One or more request fields are invalid.',
      code: 'validation_failed',
      errors: { email: ['invalid_format'] },
    }, 400, 'application/problem+json'));

    const result = await client.login('bad', 'password');

    expect(result).toMatchObject({
      status: 400,
      error: 'One or more request fields are invalid.',
      code: 'validation_failed',
      errors: { email: ['invalid_format'] },
    });
  });

  it.each([
    ['text', new Response('upstream unavailable', { status: 503, headers: { 'Content-Type': 'text/plain' } }), 'upstream unavailable'],
    ['malformed JSON', new Response('{broken', { status: 409, headers: { 'Content-Type': 'application/problem+json' } }), 'Request failed with status 409'],
    ['empty', new Response(null, { status: 403 }), 'Request failed with status 403'],
  ])('handles %s failures without masking HTTP status', async (_label, response, message) => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(response);
    const result = await client.login('person@example.com', 'correct horse battery staple');
    expect(result.status).toBe(response.status);
    expect(result.error).toBe(message);
  });

  it('handles a 204 response without parsing failure', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 204 }));
    await expect(client.deleteMyProfile()).resolves.toEqual({ status: 204 });
  });

  it('sends canonical auth and recovery payloads and reset idempotency key', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(jsonResponse({ accessToken: 'jwt', tokenType: 'Bearer', expiresAt: '2026-01-01T00:00:00Z', profile: { profileId: 'p1', email: 'a@b.co', fullName: 'A', contactNumber: null, userType: 'client', createdAt: '2026-01-01T00:00:00Z' } }, 201))
      .mockResolvedValueOnce(jsonResponse({ valid: true }))
      .mockResolvedValueOnce(jsonResponse({ success: true, message: 'Password reset completed.' }));

    await client.register({ email: 'a@b.co', fullName: 'A', password: 'correct horse battery staple', userType: 'client', contactNumber: null });
    await client.validateResetToken('token');
    const resetOperationKey = createIdempotencyKey();
    await client.resetPassword('token', 'another correct horse battery staple', resetOperationKey);

    expect(fetchMock.mock.calls[0]).toMatchObject(['http://localhost:5000/auth/register', { method: 'POST', body: JSON.stringify({ email: 'a@b.co', fullName: 'A', password: 'correct horse battery staple', userType: 'client', contactNumber: null }) }]);
    expect(fetchMock.mock.calls[1]).toMatchObject(['http://localhost:5000/auth/validate-reset-token', { method: 'POST', body: JSON.stringify({ token: 'token' }) }]);
    const resetOptions = fetchMock.mock.calls[2][1] as RequestInit;
    expect(resetOptions.method).toBe('POST');
    expect(resetOptions.body).toBe(JSON.stringify({ token: 'token', newPassword: 'another correct horse battery staple' }));
    expect(new Headers(resetOptions.headers).get('Idempotency-Key')).toBe(resetOperationKey);
    expect(resetOperationKey).toMatch(/^[A-Za-z0-9_-]{32}$/);
  });

  it('requests server-calculated availability rather than using mock slots', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse({ serviceId: 'svc', businessId: 'business', timeZone: 'Africa/Johannesburg', from: '2026-08-14T00:00:00Z', to: '2026-08-15T00:00:00Z', durationMinutes: 60, slots: [] }));
    await client.getServiceAvailability('svc', '2026-08-14T00:00:00Z', '2026-08-15T00:00:00Z');
    expect(fetchMock.mock.calls[0][0]).toBe('http://localhost:5000/services/svc/availability?from=2026-08-14T00%3A00%3A00Z&to=2026-08-15T00%3A00%3A00Z');
  });

  it('uses subject-scoped dashboard, profile, business, and booking routes', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(async () => jsonResponse({ items: [], nextCursor: null }));

    await client.getDashboard();
    await client.updateProfile({ fullName: 'Updated' });
    await client.getMyBusinesses();
    await client.getClientBookings();
    await client.getProviderBookings();
    await client.createBooking({ serviceId: 'svc', startTime: '2026-08-14T10:00:00Z' }, 'same-booking-operation-key');
    await client.performBookingAction('booking', { action: 'confirm', expectedVersion: 2 }, 'same-action-operation-key');

    expect(fetchMock.mock.calls.map(([url]) => url)).toEqual([
      'http://localhost:5000/dashboard/me',
      'http://localhost:5000/profiles/me',
      'http://localhost:5000/businesses/mine',
      'http://localhost:5000/bookings/client/me',
      'http://localhost:5000/bookings/provider/me',
      'http://localhost:5000/bookings',
      'http://localhost:5000/bookings/booking/actions',
    ]);
    expect(JSON.parse(fetchMock.mock.calls[5][1]!.body as string)).toEqual({ serviceId: 'svc', startTime: '2026-08-14T10:00:00Z' });
    expect(new Headers(fetchMock.mock.calls[5][1]!.headers).get('Idempotency-Key')).toBe('same-booking-operation-key');
    expect(new Headers(fetchMock.mock.calls[6][1]!.headers).get('Idempotency-Key')).toBe('same-action-operation-key');
  });
});
