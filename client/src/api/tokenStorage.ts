import type { AuthResponse, CurrentUser } from '../types/api';

/**
 * Session persistence.
 *
 * Tokens live in localStorage, which is readable by any script on the origin —
 * an XSS bug would expose them. The alternative, httpOnly cookies, needs CSRF
 * protection and a same-site deployment. This is the standard trade-off for a
 * token-based SPA; the mitigations that matter are a short access-token life
 * (15 min) and refresh rotation with reuse detection, both already in place.
 */
const ACCESS_TOKEN_KEY = 'qc.accessToken';
const REFRESH_TOKEN_KEY = 'qc.refreshToken';
const USER_KEY = 'qc.user';

export const tokenStorage = {
  getAccessToken: (): string | null => localStorage.getItem(ACCESS_TOKEN_KEY),

  getRefreshToken: (): string | null => localStorage.getItem(REFRESH_TOKEN_KEY),

  getUser: (): CurrentUser | null => {
    const raw = localStorage.getItem(USER_KEY);

    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as CurrentUser;
    } catch {
      // Corrupt entry: drop it rather than crash the app on every load.
      localStorage.removeItem(USER_KEY);
      return null;
    }
  },

  save: (auth: AuthResponse): void => {
    localStorage.setItem(ACCESS_TOKEN_KEY, auth.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, auth.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(auth.user));
  },

  saveUser: (user: CurrentUser): void => {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  },

  clear: (): void => {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  },
};
