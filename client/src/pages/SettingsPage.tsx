import { useState } from 'react';
import { useForm } from 'react-hook-form';
import {
  Alert,
  Button,
  Card,
  CardContent,
  Divider,
  Grid,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { authApi } from '../api/endpoints';
import { getFieldErrors } from '../api/client';
import { PageHeader } from '../components/PageHeader';
import { useAuth } from '../contexts/AuthContext';
import { useNotification } from '../contexts/NotificationContext';

interface PasswordForm {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

function ProfileField({ label, value }: { label: string; value?: string | null }) {
  return (
    <Grid item xs={12} sm={6}>
      <Typography variant="caption" color="text.secondary" display="block">
        {label}
      </Typography>
      <Typography variant="body2">{value ?? '—'}</Typography>
    </Grid>
  );
}

export function SettingsPage() {
  const { user } = useAuth();
  const { notifySuccess, notifyError } = useNotification();
  const [changed, setChanged] = useState(false);

  const {
    register,
    handleSubmit,
    reset,
    watch,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<PasswordForm>({
    defaultValues: { currentPassword: '', newPassword: '', confirmPassword: '' },
  });

  async function onSubmit(form: PasswordForm) {
    try {
      await authApi.changePassword(form.currentPassword, form.newPassword);
      notifySuccess('Пароль изменён. Остальные сессии завершены.');
      reset();
      setChanged(true);
    } catch (error) {
      const fieldErrors = getFieldErrors(error);

      if (fieldErrors.currentPassword) {
        setError('currentPassword', { message: fieldErrors.currentPassword });
      } else if (fieldErrors.newPassword) {
        setError('newPassword', { message: fieldErrors.newPassword });
      } else {
        notifyError(error);
      }
    }
  }

  return (
    <>
      <PageHeader title="Настройки" subtitle="Профиль и безопасность учётной записи" />

      <Grid container spacing={2}>
        <Grid item xs={12} md={6}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Профиль
              </Typography>

              <Grid container spacing={2} sx={{ mt: 0.5 }}>
                <ProfileField label="Имя пользователя" value={user?.userName} />
                <ProfileField label="E-mail" value={user?.email} />
                <ProfileField label="ФИО" value={user?.fullName} />
                <ProfileField label="Должность" value={user?.position} />
                <ProfileField label="Подразделение" value={user?.departmentName} />
                <ProfileField label="Роль" value={user?.roleDisplayName} />
              </Grid>

              <Divider sx={{ my: 2 }} />

              <Typography variant="caption" color="text.secondary">
                Изменение профиля и прав доступа выполняет администратор системы.
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={6}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Смена пароля
              </Typography>

              {changed ? (
                <Alert severity="success" sx={{ mb: 2 }}>
                  Пароль успешно изменён. Все остальные сессии этой учётной записи завершены.
                </Alert>
              ) : null}

              <form onSubmit={handleSubmit(onSubmit)} noValidate>
                <Stack spacing={2} sx={{ mt: 1 }}>
                  <TextField
                    label="Текущий пароль"
                    type="password"
                    fullWidth
                    autoComplete="current-password"
                    error={Boolean(errors.currentPassword)}
                    helperText={errors.currentPassword?.message}
                    {...register('currentPassword', { required: 'Укажите текущий пароль.' })}
                  />

                  <TextField
                    label="Новый пароль"
                    type="password"
                    fullWidth
                    autoComplete="new-password"
                    error={Boolean(errors.newPassword)}
                    helperText={
                      errors.newPassword?.message ??
                      'Не менее 8 символов, минимум одна цифра, заглавная и строчная буква.'
                    }
                    {...register('newPassword', {
                      required: 'Укажите новый пароль.',
                      minLength: { value: 8, message: 'Пароль должен содержать не менее 8 символов.' },
                      validate: {
                        digit: (v) => /\d/.test(v) || 'Пароль должен содержать хотя бы одну цифру.',
                        upper: (v) => /[A-ZА-Я]/.test(v) || 'Пароль должен содержать заглавную букву.',
                        lower: (v) => /[a-zа-я]/.test(v) || 'Пароль должен содержать строчную букву.',
                        different: (v) =>
                          v !== watch('currentPassword') || 'Новый пароль должен отличаться от текущего.',
                      },
                    })}
                  />

                  <TextField
                    label="Повторите новый пароль"
                    type="password"
                    fullWidth
                    autoComplete="new-password"
                    error={Boolean(errors.confirmPassword)}
                    helperText={errors.confirmPassword?.message}
                    {...register('confirmPassword', {
                      required: 'Повторите новый пароль.',
                      validate: (v) => v === watch('newPassword') || 'Пароли не совпадают.',
                    })}
                  />

                  <Button type="submit" variant="contained" disabled={isSubmitting} sx={{ alignSelf: 'flex-start' }}>
                    {isSubmitting ? 'Сохранение…' : 'Изменить пароль'}
                  </Button>
                </Stack>
              </form>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </>
  );
}
