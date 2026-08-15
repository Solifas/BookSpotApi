export const bookingErrorMessage = ({ status, code, error }: { status: number; code?: string; error?: string }): string => {
  if (status === 401) return 'Sign in again before retrying.';
  if (status === 403) return 'Your account is not allowed to make this booking.';
  if (status === 400) return error || 'Choose a valid service, date, and available time.';
  if (status === 409 && code === 'booking_slot_conflict') return 'That time slot was just reserved. Choose another available time.';
  if (status === 409) return 'This booking changed. Refresh and review the latest details.';
  if (status === 503) return 'Booking is temporarily unavailable. Retry in a moment.';
  return error || 'The booking request failed. Please try again.';
};
