import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import { authApi } from '../../api/authApi';
import { TOKEN_STORAGE_KEY } from '../../api/axios';

export type AuthUser = {
  userName: string | null;
  email: string;
  roles: string[];
  message?: string;
};

type AuthContextValue = {
  token: string | null;
  user: AuthUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  hydrate: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem(TOKEN_STORAGE_KEY));
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const setAuthToken = useCallback((nextToken: string | null) => {
    setToken(nextToken);

    if (nextToken) {
      localStorage.setItem(TOKEN_STORAGE_KEY, nextToken);
    } else {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
    }
  }, []);

  const logout = useCallback(() => {
    setAuthToken(null);
    setUser(null);
  }, [setAuthToken]);

  const hydrate = useCallback(async () => {
    const storedToken = localStorage.getItem(TOKEN_STORAGE_KEY);

    if (!storedToken) {
      setUser(null);
      setToken(null);
      setIsLoading(false);
      return;
    }

    try {
      const response = await authApi.me();
      setToken(storedToken);
      setUser(response.data);
    } catch {
      setAuthToken(null);
      setUser(null);
    } finally {
      setIsLoading(false);
    }
  }, [setAuthToken]);

  const login = useCallback(async (email: string, password: string) => {
    const response = await authApi.login({ email, password });
    const nextToken = response.data.token;

    setAuthToken(nextToken);

    const meResponse = await authApi.me();
    setUser(meResponse.data);
    setToken(nextToken);
  }, [setAuthToken]);

  useEffect(() => {
    void hydrate();
  }, [hydrate]);

  const value = useMemo<AuthContextValue>(
    () => ({
      token,
      user,
      isAuthenticated: Boolean(token && user),
      isLoading,
      login,
      logout,
      hydrate,
    }),
    [hydrate, login, logout, token, user, isLoading],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }

  return context;
}
