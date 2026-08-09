
import React, { createContext, useContext, useState, ReactNode, useEffect } from 'react';
import { apiClient } from '../services/api';
import { AuthResponse, RegisterRequest } from '../types/api';

interface User {
  id: string;
  name: string;
  email: string;
  type: 'client' | 'provider';
  contactNumber?: string;
}

interface AuthContextType {
  user: User | null;
  login: (email: string, password: string) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => void;
  isLoggedIn: boolean;
  loading: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider = ({ children }: AuthProviderProps) => {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  // Check for existing token on app load
  useEffect(() => {
    const initAuth = async () => {
      const token = localStorage.getItem('authToken');
      if (token) {
        try {
          const response = await apiClient.getProfile();
          console.log('user', response.data)
          if (response.data) {
            setUser({
              id: response.data.id,
              name: response.data.fullName,
              email: response.data.email,
              type: response.data.userType as 'client' | 'provider',
              contactNumber: response.data.contactNumber,
            });
          }
        } catch (error) {
          console.error('Failed to restore auth session:', error);
          localStorage.removeItem('authToken');
        }
      }
      setLoading(false);
    };

    initAuth();
  }, []);

  const login = async (email: string, password: string) => {
    setLoading(true);
    try {
      const response = await apiClient.login(email, password);

      if (response.error) {
        throw new Error(response.error);
      }

      if (response.data) {
        apiClient.setToken(response.data.token);
        setUser({
          id: response.data.userId,
          name: response.data.fullName,
          email: response.data.email,
          type: response.data.userType as 'client' | 'provider',
          contactNumber: response.data.contactNumber,
        });
      }
    } finally {
      setLoading(false);
    }
  };

  const register = async (data: RegisterRequest) => {
    setLoading(true);
    try {
      const response = await apiClient.register(data);

      if (response.error) {
        throw new Error(response.error);
      }

      if (response.data) {
        apiClient.setToken(response.data.token);
        setUser({
          id: response.data.userId,
          name: response.data.fullName,
          email: response.data.email,
          type: response.data.userType as 'client' | 'provider',
          contactNumber: response.data.contactNumber,
        });
      }
    } finally {
      setLoading(false);
    }
  };

  const logout = () => {
    apiClient.clearToken();
    setUser(null);
  };

  const isLoggedIn = !!user;

  return (
    <AuthContext.Provider value={{ user, login, register, logout, isLoggedIn, loading }}>
      {children}
    </AuthContext.Provider>
  );
};
