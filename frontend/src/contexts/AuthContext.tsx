import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { apiClient } from '../services/api';
import type { Profile, RegisterRequest, UserTypeValue } from '../types/api';

export interface User {
  id: string;
  name: string;
  email: string;
  type: UserTypeValue;
  contactNumber: string | null;
}

interface AuthContextType {
  user: User | null;
  login: (email: string, password: string) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  refreshProfile: () => Promise<void>;
  logout: () => void;
  isLoggedIn: boolean;
  loading: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const toUser = (profile: Profile): User => ({
  id: profile.profileId,
  name: profile.fullName,
  email: profile.email,
  type: profile.userType,
  contactNumber: profile.contactNumber,
});

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within an AuthProvider');
  return context;
};

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  const refreshProfile = async () => {
    const response = await apiClient.getProfile();
    if (response.error || !response.data) {
      if (response.status === 401) {
        apiClient.clearToken();
        setUser(null);
      }
      throw new Error(response.error || 'Unable to restore your session.');
    }
    setUser(toUser(response.data));
  };

  useEffect(() => {
    const initialise = async () => {
      if (apiClient.hasToken()) {
        try {
          await refreshProfile();
        } catch {
          // Invalid sessions are cleared; transient failures remain retryable.
        }
      }
      setLoading(false);
    };
    void initialise();
  }, []);

  const login = async (email: string, password: string) => {
    setLoading(true);
    try {
      const response = await apiClient.login(email, password);
      if (response.error || !response.data) throw new Error(response.error || 'Login failed.');
      apiClient.setToken(response.data.accessToken);
      setUser(toUser(response.data.profile));
    } finally {
      setLoading(false);
    }
  };

  const register = async (data: RegisterRequest) => {
    setLoading(true);
    try {
      const response = await apiClient.register(data);
      if (response.error || !response.data) throw new Error(response.error || 'Registration failed.');
      apiClient.setToken(response.data.accessToken);
      setUser(toUser(response.data.profile));
    } finally {
      setLoading(false);
    }
  };

  const logout = () => {
    apiClient.clearToken();
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{
      user,
      login,
      register,
      refreshProfile,
      logout,
      isLoggedIn: Boolean(user),
      loading,
    }}>
      {children}
    </AuthContext.Provider>
  );
};
