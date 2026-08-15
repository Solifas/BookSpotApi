import { describe, expect, it } from 'vitest';
import { bookingErrorMessage } from './bookingErrors';

describe('bookingErrorMessage', () => {
  it.each([
    [401, 'authentication_required', 'Sign in again before retrying.'],
    [403, 'role_forbidden', 'Your account is not allowed to make this booking.'],
    [400, 'validation_failed', 'Choose a valid service, date, and available time.'],
    [409, 'booking_slot_conflict', 'That time slot was just reserved. Choose another available time.'],
    [409, 'booking_state_conflict', 'This booking changed. Refresh and review the latest details.'],
    [503, 'persistence_unavailable', 'Booking is temporarily unavailable. Retry in a moment.'],
  ])('maps %s %s to an actionable message', (status, code, expected) => {
    expect(bookingErrorMessage({ status, code })).toBe(expected);
  });
});
