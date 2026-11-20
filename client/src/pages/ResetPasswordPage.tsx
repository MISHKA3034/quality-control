import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link as RouterLink, useNavigate, useSearchParams } from 'react-router-dom';
import { Alert, Box, Button, Card, CardContent, Link, Stack, TextField, Typography } from '@mui/material';
import { getErrorMessage } from '../api/client';
import { authApi } from '../api/endpoints';

interface ResetForm {
  newPassword: string;
  confirmPassword: string;
}

export function ResetPasswordPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState(false);

  // Both arrive in the emailed link; NotificationService builds it this way.
  const email = searchParams.get('email') ?? '';
  const token = searchParams.get('token') ?? '';

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<ResetForm>({ defaultValues: { newPassword: '', confirmPassword: '' } });

  async function onSubmit(data: ResetForm) {
    setError(null);

    try {
      await authApi.resetPassword(email, token, data.newPassword);
      setDone(true);
      setTimeout(() => navigate('/login', { replace: true }), 2500);
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  const linkIsUsable = email !== '' && token !== '';

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
          <Typography variant="h5" gutterBottom>
            Новый пароль
          </Typography>

          {!linkIsUsable ? (
            <>
              <Alert severity="error" sx={{ my: 2 }}>
                Ссылка неполная или повреждена. Запросите восстановление пароля заново.
              </Alert>
              <Link component={RouterLink} to="/forgot-password" variant="body2">
                Запросить новую ссылку
              </Link>
            </>
          ) : done ? (
            <Alert severity="success" sx={{ my: 2 }}>
              Пароль изменён. Все активные сессии завершены. Сейчас откроется страница входа…
            </Alert>
          ) : (
            <>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                Задайте новый пароль для учётной записи <strong>{email}</strong>.
              </Typography>

              {error ? (
                <Alert severity="error" sx={{ mb: 2 }}>
                  {error}
                </Alert>
              ) : null}

              <form onSubmit={handleSubmit(onSubmit)} noValidate>
                <Stack spacing={2}>
                  <TextField
                    label="Новый пароль"
                    type="password"
                    fullWidth
                    autoFocus
                    autoComplete="new-password"
                    error={Boolean(errors.newPassword)}
                    helperText={
                      errors.newPassword?.message ??
                      'Не менее 8 символов, минимум одна цифра, заглавная и строчная буква.'
                    }
                    {...register('newPassword', {
                      required: 'Укажите новый пароль.',
                      // Mirrors the server policy so the user is told before a round-trip.
                      minLength: { value: 8, message: 'Пароль должен содержать не менее 8 символов.' },
                      validate: {
                        digit: (v) => /\d/.test(v) || 'Пароль должен содержать хотя бы одну цифру.',
                        upper: (v) => /[A-ZА-Я]/.test(v) || 'Пароль должен содержать заглавную букву.',
                        lower: (v) => /[a-zа-я]/.test(v) || 'Пароль должен содержать строчную букву.',
                      },
                    })}
                  />

                  <TextField
                    label="Повторите пароль"
                    type="password"
                    fullWidth
                    autoComplete="new-password"
                    error={Boolean(errors.confirmPassword)}
                    helperText={errors.confirmPassword?.message}
                    {...register('confirmPassword', {
                      required: 'Повторите пароль.',
                      validate: (value) => value === watch('newPassword') || 'Пароли не совпадают.',
                    })}
                  />

                  <Button type="submit" variant="contained" disabled={isSubmitting} fullWidth>
                    {isSubmitting ? 'Сохранение…' : 'Сохранить пароль'}
                  </Button>
                </Stack>
              </form>
            </>
          )}
        </CardContent>
      </Card>
    </Box>
  );
}
