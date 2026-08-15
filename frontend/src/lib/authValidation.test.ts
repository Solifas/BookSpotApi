import { describe, expect, it } from 'vitest';
import { getResetTokenFromLocation, validateRegistration, validateResetPassword } from './authValidation';

describe('auth validation', () => {
  it('accepts valid client and provider registrations', () => {
    const base = { fullName: 'A Person', email: 'person@example.com', contactNumber: '+27 82 123 4567', password: 'a long safe password', confirmPassword: 'a long safe password' };
    expect(validateRegistration({ ...base, userType: 'client' })).toEqual({});
    expect(validateRegistration({ ...base, userType: 'provider' })).toEqual({});
  });

  it('returns actionable registration field errors', () => {
    expect(validateRegistration({ fullName: '', email: 'bad', contactNumber: '123', password: 'short', confirmPassword: 'other', userType: 'client' })).toEqual({
      fullName: 'Enter your full name.',
      email: 'Enter a valid email address.',
      contactNumber: 'Enter a valid contact number or leave it blank.',
      password: 'Use 15 to 64 characters for your password.',
      confirmPassword: 'Passwords do not match.',
    });
  });

  it('validates reset password using the same policy', () => {
    expect(validateResetPassword('short', 'different')).toEqual({
      password: 'Use 15 to 64 characters for your password.',
      confirmPassword: 'Passwords do not match.',
    });
  });

  it('prefers fragment reset tokens and rejects query-string capabilities', () => {
    expect(getResetTokenFromLocation('#token=fragment-secret', '?token=query-secret')).toBe('fragment-secret');
    expect(getResetTokenFromLocation('#fragment-secret', '')).toBe('fragment-secret');
    expect(getResetTokenFromLocation('', '?token=query-secret')).toBeNull();
  });
});
