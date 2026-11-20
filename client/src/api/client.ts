import axios, {
  AxiosError,
  type AxiosInstance,
  type AxiosRequestConfig,
  type InternalAxiosRequestConfig,
} from 'axios';
import type { AuthResponse, ProblemDetails } from '../types/api';
import { tokenStorage } from './tokenStorage';

/** Same-origin: Vite proxies /api to the backend in dev, and a reverse proxy does in production. */
const BASE_URL = '/api';

export const apiClient: AxiosInstance = axios.create({
  baseURL: BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

/** Marks a request that has already been retried, so a failed retry cannot loop. */
interface RetryableConfig extends InternalAxiosRequestConfig {
  _retried?: boolean;
}

apiClient.interceptors.request.use((config) => {
  const token = tokenStorage.getAccessToken();

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

/**
 * In-flight refresh, shared by every request that hits a 401 at once.
 *
 * Without this, a screen firing five parallel requests on an expired token would
 * start five refreshes. Refresh tokens rotate and reuse is treated as theft, so
 * the second one to arrive would revoke the whole chain and sign the user out —
 * the exact failure this exists to prevent.
 */
let refreshPromise: Promise<string> | null = null;

/** Lets AuthContext react to a session that cannot be recovered. */
let onSessionExpired: (() => void) | null = null;

export function setSessionExpiredHandler(handler: () => void): void {
  onSessionExpired = handler;
}

async function refreshAccessToken(): Promise<string> {
  const refreshToken = tokenStorage.getRefreshToken();

  if (!refreshToken) {
    throw new Error('Отсутствует токен обновления.');
  }

  // A bare axios call, not apiClient: going through the instance would attach the
  // dead access token and re-enter this interceptor on failure.
  const { data } = await axios.post<AuthResponse>(`${BASE_URL}/auth/refresh`, { refreshToken });

  tokenStorage.save(data);
  return data.accessToken;
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ProblemDetails>) => {
    const config = error.config as RetryableConfig | undefined;

    const shouldRefresh =
      error.response?.status === 401 &&
      config &&
      !config._retried &&
      // The refresh endpoint returning 401 means the refresh token itself is dead.
      !config.url?.includes('/auth/refresh') &&
      !config.url?.includes('/auth/login');

    if (!shouldRefresh) {
      return Promise.reject(error);
    }

    config._retried = true;

    try {
      refreshPromise ??= refreshAccessToken().finally(() => {
        refreshPromise = null;
      });

      const token = await refreshPromise;

      config.headers.Authorization = `Bearer ${token}`;
      return await apiClient.request(config);
    } catch (refreshError) {
      tokenStorage.clear();
      onSessionExpired?.();
      return Promise.reject(refreshError);
    }
  },
);

/**
 * Extracts a message worth showing a user from an API error.
 *
 * The API returns RFC 7807 problem responses, so the useful text is in `detail`
 * or in per-field `errors` — not in Axios's own "Request failed with status 400".
 */
export function getErrorMessage(error: unknown): string {
  if (axios.isAxiosError<ProblemDetails>(error)) {
    const problem = error.response?.data;

    if (problem?.errors) {
      const messages = Object.values(problem.errors).flat();

      if (messages.length > 0) {
        return messages.join(' ');
      }
    }

    if (problem?.detail) {
      return problem.detail;
    }

    if (problem?.title) {
      return problem.title;
    }

    if (error.code === 'ERR_NETWORK') {
      return 'Сервер недоступен. Проверьте подключение.';
    }
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'Произошла непредвиденная ошибка.';
}

/** Per-field validation errors, for feeding back into a form. */
export function getFieldErrors(error: unknown): Record<string, string> {
  if (!axios.isAxiosError<ProblemDetails>(error) || !error.response?.data?.errors) {
    return {};
  }

  const result: Record<string, string> = {};

  for (const [field, messages] of Object.entries(error.response.data.errors)) {
    // The server names fields as they appear on the C# DTO (PascalCase); the
    // forms use camelCase.
    const key = field.charAt(0).toLowerCase() + field.slice(1);
    result[key] = messages.join(' ');
  }

  return result;
}

/**
 * Downloads a file and hands it to the browser.
 *
 * Needed because report endpoints are authenticated: a plain link or window.open
 * would send no Authorization header and get a 401.
 */
export async function downloadFile(url: string, config?: AxiosRequestConfig): Promise<void> {
  const response = await apiClient.get<Blob>(url, { ...config, responseType: 'blob' });

  const disposition = response.headers['content-disposition'] as string | undefined;
  const fileName = parseFileName(disposition) ?? 'report';

  const blobUrl = URL.createObjectURL(response.data);
  const link = document.createElement('a');

  link.href = blobUrl;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();

  // Releases the blob; without this the file stays in memory until a full reload.
  URL.revokeObjectURL(blobUrl);
}

function parseFileName(disposition: string | undefined): string | null {
  if (!disposition) {
    return null;
  }

  // filename*=UTF-8''... carries the Cyrillic name; plain filename= is the ASCII
  // fallback the server sends alongside it.
  const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(disposition);

  if (utf8Match?.[1]) {
    return decodeURIComponent(utf8Match[1]);
  }

  const plainMatch = /filename="?([^";]+)"?/i.exec(disposition);
  return plainMatch?.[1] ?? null;
}
