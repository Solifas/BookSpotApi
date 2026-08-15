import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import ResetPassword from './ResetPassword';
import { apiClient } from '../services/api';

describe('ResetPassword', () => {
  beforeEach(() => {
    window.history.replaceState(null, '', '/reset-password#token=reset-secret');
    vi.spyOn(apiClient, 'validateResetToken').mockResolvedValue({ status: 200, data: { valid: true } });
  });

  it('offers a retry when token validation is temporarily unavailable', async () => {
    vi.mocked(apiClient.validateResetToken)
      .mockResolvedValueOnce({ status: 503, code: 'persistence_unavailable', error: 'Unavailable' })
      .mockResolvedValueOnce({ status: 200, data: { valid: true } });
    render(<MemoryRouter><ResetPassword /></MemoryRouter>);
    fireEvent.click(await screen.findByRole('button', { name: 'Try validation again' }));
    expect(await screen.findByLabelText('New password')).toBeInTheDocument();
  });

  it('validates a fragment token, clears it from the URL, and completes reset', async () => {
    const reset = vi.spyOn(apiClient, 'resetPassword').mockResolvedValue({ status: 200, data: { success: true, message: 'Password reset completed.' } });
    render(<MemoryRouter><ResetPassword /></MemoryRouter>);
    expect(await screen.findByLabelText('New password')).toBeInTheDocument();
    expect(window.location.hash).toBe('');
    fireEvent.change(screen.getByLabelText('New password'), { target: { value: 'another safe password' } });
    fireEvent.change(screen.getByLabelText('Confirm new password'), { target: { value: 'another safe password' } });
    fireEvent.click(screen.getByRole('button', { name: 'Reset password' }));
    await waitFor(() => expect(reset).toHaveBeenCalledWith('reset-secret', 'another safe password', expect.stringMatching(/^[A-Za-z0-9_-]{32}$/)));
    expect(await screen.findByText('Password reset completed.')).toBeInTheDocument();
  });
});
