import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react';
import { Alert, Snackbar } from '@mui/material';
import type { AlertColor } from '@mui/material';
import { getErrorMessage } from '../api/client';

interface Notification {
  message: string;
  severity: AlertColor;
}

interface NotificationContextValue {
  notify: (message: string, severity?: AlertColor) => void;
  notifySuccess: (message: string) => void;
  /** Extracts a readable message from an API error and shows it. */
  notifyError: (error: unknown) => void;
}

const NotificationContext = createContext<NotificationContextValue | undefined>(undefined);

export function NotificationProvider({ children }: { children: ReactNode }) {
  const [notification, setNotification] = useState<Notification | null>(null);

  const notify = useCallback((message: string, severity: AlertColor = 'info') => {
    setNotification({ message, severity });
  }, []);

  const notifySuccess = useCallback((message: string) => {
    setNotification({ message, severity: 'success' });
  }, []);

  const notifyError = useCallback((error: unknown) => {
    setNotification({ message: getErrorMessage(error), severity: 'error' });
  }, []);

  const value = useMemo(
    () => ({ notify, notifySuccess, notifyError }),
    [notify, notifySuccess, notifyError],
  );

  return (
    <NotificationContext.Provider value={value}>
      {children}

      <Snackbar
        open={notification !== null}
        // Errors stay longer: they usually carry something to read and act on.
        autoHideDuration={notification?.severity === 'error' ? 8000 : 4000}
        onClose={() => setNotification(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      >
        {notification ? (
          <Alert
            onClose={() => setNotification(null)}
            severity={notification.severity}
            variant="filled"
            sx={{ maxWidth: 480 }}
          >
            {notification.message}
          </Alert>
        ) : undefined}
      </Snackbar>
    </NotificationContext.Provider>
  );
}

export function useNotification(): NotificationContextValue {
  const context = useContext(NotificationContext);

  if (!context) {
    throw new Error('useNotification должен вызываться внутри NotificationProvider.');
  }

  return context;
}
