import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link as RouterLink, Navigate, useLocation, useNavigate } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Link,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { getErrorMessage } from '../api/client';
import { useAuth } from '../contexts/AuthContext';

interface LoginForm {
  userName: string;
  password: string;
}

export function LoginPage() {
  const { login, isAuthenticated, isLoading } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [error, setError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({ defaultValues: { userName: '', password: '' } });

  // Already signed in — go where they were headed, or to the dashboard.
  if (!isLoading && isAuthenticated) {
    const from = (location.state as { from?: Location } | null)?.from?.pathname ?? '/';
    return <Navigate to={from} replace />;
  }

  async function onSubmit(data: LoginForm) {
    setError(null);

    try {
      await login(data.userName, data.password);
      const from = (location.state as { from?: Location } | null)?.from?.pathname ?? '/';
      navigate(from, { replace: true });
    } catch (err) {
      // Shown inline rather than as a toast: it belongs next to the fields.
      setError(getErrorMessage(err));
    }
  }

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        bgcolor: 'background.default',
        p: 2,
      }}
    >
      <Card sx={{ width: '100%', maxWidth: 420 }}>
        <CardContent sx={{ p: 4 }}>
          <Stack spacing={1} sx={{ mb: 3, textAlign: 'center' }}>
            <Typography variant="h5">АСКК</Typography>
            <Typography variant="body2" color="text.secondary">
              Автоматизированная система контроля качества продукции
            </Typography>
            <Typography variant="caption" color="text.secondary">
              ОАО «Амкодор-Пинск»
            </Typography>
          </Stack>

          {error ? (
            <Alert severity="error" sx={{ mb: 2 }}>
              {error}
            </Alert>
          ) : null}

          <form onSubmit={handleSubmit(onSubmit)} noValidate>
            <Stack spacing={2}>
              <TextField
                label="Имя пользователя или e-mail"
                fullWidth
                autoFocus
                autoComplete="username"
                error={Boolean(errors.userName)}
                helperText={errors.userName?.message}
                {...register('userName', { required: 'Укажите имя пользователя.' })}
              />

              <TextField
                label="Пароль"
                type="password"
                fullWidth
                autoComplete="current-password"
                error={Boolean(errors.password)}
                helperText={errors.password?.message}
                {...register('password', { required: 'Укажите пароль.' })}
              />

              <Button type="submit" variant="contained" size="large" disabled={isSubmitting} fullWidth>
                {isSubmitting ? 'Вход…' : 'Войти'}
              </Button>

              <Link component={RouterLink} to="/forgot-password" variant="body2" textAlign="center">
                Забыли пароль?
              </Link>
            </Stack>
          </form>
        </CardContent>
      </Card>
    </Box>
  );
}
