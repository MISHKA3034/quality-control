import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link as RouterLink } from 'react-router-dom';
import { Alert, Box, Button, Card, CardContent, Link, Stack, TextField, Typography } from '@mui/material';
import { getErrorMessage } from '../api/client';
import { authApi } from '../api/endpoints';

export function ForgotPasswordPage() {
  const [sent, setSent] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<{ email: string }>({ defaultValues: { email: '' } });

  async function onSubmit(data: { email: string }) {
    setError(null);

    try {
      await authApi.forgotPassword(data.email);
      setSent(true);
    } catch (err) {
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
          <Typography variant="h5" gutterBottom>
            Восстановление пароля
          </Typography>

          {sent ? (
            <>
              {/* Worded so it says the same thing whether or not the address exists.
                  The API deliberately does not reveal which, and neither should this. */}
              <Alert severity="success" sx={{ my: 2 }}>
                Если указанный адрес зарегистрирован в системе, на него отправлено письмо
                со ссылкой для сброса пароля. Ссылка действительна ограниченное время.
              </Alert>

              <Link component={RouterLink} to="/login" variant="body2">
                Вернуться ко входу
              </Link>
            </>
          ) : (
            <>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                Укажите e-mail вашей учётной записи — мы отправим ссылку для установки нового пароля.
              </Typography>

              {error ? (
                <Alert severity="error" sx={{ mb: 2 }}>
                  {error}
                </Alert>
              ) : null}

              <form onSubmit={handleSubmit(onSubmit)} noValidate>
                <Stack spacing={2}>
                  <TextField
                    label="E-mail"
                    type="email"
                    fullWidth
                    autoFocus
                    error={Boolean(errors.email)}
                    helperText={errors.email?.message}
                    {...register('email', {
                      required: 'Укажите e-mail.',
                      pattern: { value: /^\S+@\S+\.\S+$/, message: 'Некорректный формат e-mail.' },
                    })}
                  />

                  <Button type="submit" variant="contained" disabled={isSubmitting} fullWidth>
                    {isSubmitting ? 'Отправка…' : 'Отправить ссылку'}
                  </Button>

                  <Link component={RouterLink} to="/login" variant="body2" textAlign="center">
                    Вернуться ко входу
                  </Link>
                </Stack>
              </form>
            </>
          )}
        </CardContent>
      </Card>
    </Box>
  );
}
