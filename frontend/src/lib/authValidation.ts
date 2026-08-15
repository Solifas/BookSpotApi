import type { UserTypeValue } from '../types/api';

export interface RegistrationFields {
  fullName: string;
  email: string;
  contactNumber: string;
  password: string;
  confirmPassword: string;
  userType: UserTypeValue;
}

export type FieldErrors = Record<string, string>;

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const phonePattern = /^\+?[0-9][0-9 ()-]{5,30}[0-9]$/;

export const validateRegistration = (fields: RegistrationFields): FieldErrors => {
  const errors: FieldErrors = {};
  if (!fields.fullName.trim()) errors.fullName = 'Enter your full name.';
  if (!emailPattern.test(fields.email.trim())) errors.email = 'Enter a valid email address.';
  if (fields.contactNumber.trim() && !phonePattern.test(fields.contactNumber.trim())) {
    errors.contactNumber = 'Enter a valid contact number or leave it blank.';
  }
  if (Array.from(fields.password).length < 15 || Array.from(fields.password).length > 64 || new TextEncoder().encode(fields.password).length > 72) {
    errors.password = 'Use 15 to 64 characters for your password.';
  }
  if (fields.password !== fields.confirmPassword) errors.confirmPassword = 'Passwords do not match.';
  return errors;
};

export const validateResetPassword = (password: string, confirmPassword: string): FieldErrors => {
  const errors: FieldErrors = {};
  if (Array.from(password).length < 15 || Array.from(password).length > 64 || new TextEncoder().encode(password).length > 72) {
    errors.password = 'Use 15 to 64 characters for your password.';
  }
  if (password !== confirmPassword) errors.confirmPassword = 'Passwords do not match.';
  return errors;
};

export const getResetTokenFromLocation = (hash: string, _search: string): string | null => {
  const fragment = hash.replace(/^#/, '');
  if (fragment) {
    const params = new URLSearchParams(fragment);
    const named = params.get('token');
    if (named) return named;
    if (!fragment.includes('=')) return decodeURIComponent(fragment);
  }
  return null;
};
