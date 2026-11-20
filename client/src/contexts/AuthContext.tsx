import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import { setSessionExpiredHandler } from '../api/client';
import { authApi } from '../api/endpoints';
import { tokenStorage } from '../api/tokenStorage';
import type { CurrentUser, UserRole } from '../types/api';

interface AuthContextValue {
  user: CurrentUser | null;
  isAuthenticated: boolean;
  /** True until the stored session has been checked, so routes do not flash the login page. */
  isLoading: boolean;
  login: (userName: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  hasRole: (...roles: UserRole[]) => boolean;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(() => tokenStorage.getUser());
  const [isLoading, setIsLoading] = useState(true);

  // Lets the axios interceptor drop the session when a refresh fails.
  useEffect(() => {
    setSessionExpiredHandler(() => setUser(null));
  }, []);

  useEffect(() => {
    let cancelled = false;

    async function restoreSession() {
      if (!tokenStorage.getAccessToken()) {
        setIsLoading(false);
        return;
      }

      try {
        // Revalidates against the server rather than trusting the cached user:
        // a role or an account status may have changed since the last visit.
        const current = await authApi.me();

        if (!cancelled) {
          tokenStorage.saveUser(current);
          setUser(current);
        }
      } catch {
        // The interceptor has already tried to refresh; reaching here means the
        // session is genuinely gone.
        if (!cancelled) {
          tokenStorage.clear();
          setUser(null);
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    void restoreSession();

    return () => {
      cancelled = true;
    };
  }, []);

  const login = useCallback(async (userName: string, password: string) => {
    const auth = await authApi.login(userName, password);
    tokenStorage.save(auth);
    setUser(auth.user);
  }, []);

  const logout = useCallback(async () => {
    const refreshToken = tokenStorage.getRefreshToken();

    try {
      if (refreshToken) {
        await authApi.logout(refreshToken);
      }
    } catch {
      // A failed revoke must not trap the user in a session they asked to leave;
      // the local tokens are cleared regardless.
    } finally {
      tokenStorage.clear();
      setUser(null);
    }
  }, []);

  const hasRole = useCallback(
    (...roles: UserRole[]) => (user ? roles.includes(user.role) : false),
    [user],
  );

  const value = useMemo<AuthContextValue>(
    () => ({ user, isAuthenticated: user !== null, isLoading, login, logout, hasRole }),
    [user, isLoading, login, logout, hasRole],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error('useAuth должен вызываться внутри AuthProvider.');
  }

  return context;
}
