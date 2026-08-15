import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import Register from './Register';

const register = vi.fn();
vi.mock('../contexts/AuthContext', () => ({ useAuth: () => ({ register }) }));

describe('Register', () => {
  beforeEach(() => {
    register.mockReset();
    register.mockResolvedValue(undefined);
  });

  it('submits the canonical provider registration payload when provider signup is selected', async () => {
    render(<MemoryRouter initialEntries={['/register?type=provider']}><Register /></MemoryRouter>);
    fireEvent.change(screen.getByLabelText('Full name'), { target: { value: 'Provider Person' } });
    fireEvent.change(screen.getByLabelText('Email address'), { target: { value: 'provider@example.com' } });
    fireEvent.change(screen.getByLabelText('Contact number (optional)'), { target: { value: '+27 82 123 4567' } });
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'a long safe password' } });
    fireEvent.change(screen.getByLabelText('Confirm password'), { target: { value: 'a long safe password' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create provider account' }));
    await waitFor(() => expect(register).toHaveBeenCalledWith({
      email: 'provider@example.com', fullName: 'Provider Person', contactNumber: '+27 82 123 4567',
      password: 'a long safe password', userType: 'provider',
    }));
  });

  it('shows accessible validation errors without submitting invalid client data', () => {
    render(<MemoryRouter><Register /></MemoryRouter>);
    fireEvent.click(screen.getByRole('button', { name: 'Create client account' }));
    expect(screen.getAllByRole('alert').length).toBeGreaterThan(1);
    expect(register).not.toHaveBeenCalled();
  });
});
