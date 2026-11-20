import { Box, CircularProgress } from '@mui/material';
import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import type { UserRole } from '../types/api';

interface ProtectedRouteProps {
  /** When given, the route also requires one of these roles. */
  roles?: UserRole[];
}

/**
 * Gate for authenticated routes.
 *
 * This is convenience, not security: it only decides what to render. Every
 * endpoint behind it is independently authorised on the server, so editing the
 * bundle to bypass this gains access to nothing.
 */
export function ProtectedRoute({ roles }: ProtectedRouteProps) {
  const { isAuthenticated, isLoading, user } = useAuth();
  const location = useLocation();

  // Without this, a page reload would bounce a signed-in user to the login screen
  // while the stored session is still being checked.
  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!isAuthenticated) {
    // Remembers where they were headed so login can return them there.
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (roles && user && !roles.includes(user.role)) {
    return <Navigate to="/403" replace />;
  }

  return <Outlet />;
}
