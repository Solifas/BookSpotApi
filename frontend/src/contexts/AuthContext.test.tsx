import { act, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthProvider, useAuth } from './AuthContext';
import { apiClient } from '../services/api';
import type { AuthResponse, Profile } from '../types/api';

const profile: Profile = {
  profileId: 'profile-123', email: 'client@example.com', fullName: 'Client Person',
  contactNumber: null, userType: 'client', createdAt: '2026-08-14T00:00:00Z',
};

const Consumer = () => {
  const auth = useAuth();
  return <div>
    <span data-testid="loading">{String(auth.loading)}</span>
    <span data-testid="user">{auth.user ? `${auth.user.id}:${auth.user.name}:${auth.user.type}` : 'none'}</span>
    <button onClick={() => auth.login('client@example.com', 'correct horse battery staple')}>login</button>
  </div>;
};

describe('AuthProvider', () => {
  beforeEach(() => apiClient.clearToken());

  it('uses canonical AuthResponse fields and keeps login continuity', async () => {
    vi.spyOn(apiClient, 'getProfile').mockResolvedValue({ status: 401, error: 'Authentication required' });
    vi.spyOn(apiClient, 'login').mockResolvedValue({
      status: 200,
      data: { accessToken: 'canonical-jwt', tokenType: 'Bearer', expiresAt: '2026-08-14T01:00:00Z', profile } as AuthResponse,
    });
    const setToken = vi.spyOn(apiClient, 'setToken');

    render(<AuthProvider><Consumer /></AuthProvider>);
    await waitFor(() => expect(screen.getByTestId('loading')).toHaveTextContent('false'));
    await act(async () => screen.getByRole('button', { name: 'login' }).click());

    expect(setToken).toHaveBeenCalledWith('canonical-jwt');
    expect(window.localStorage.getItem('authToken')).toBeNull();
    expect(screen.getByTestId('user')).toHaveTextContent('profile-123:Client Person:client');
  });

  it('restores an in-memory authenticated profile using profileId', async () => {
    apiClient.setToken('in-memory-token');
    vi.spyOn(apiClient, 'getProfile').mockResolvedValue({ status: 200, data: profile });

    render(<AuthProvider><Consumer /></AuthProvider>);

    await waitFor(() => expect(screen.getByTestId('user')).toHaveTextContent('profile-123:Client Person:client'));
  });
});
