import { useCallback, useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import {
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  IconButton,
  MenuItem,
  Stack,
  TextField,
  Tooltip,
} from '@mui/material';
import {
  Add as AddIcon,
  Delete as DeleteIcon,
  Edit as EditIcon,
  LockOpen as LockOpenIcon,
  Password as PasswordIcon,
} from '@mui/icons-material';
import { employeesApi, usersApi } from '../api/endpoints';
import { getFieldErrors } from '../api/client';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { DataTable, type Column } from '../components/DataTable';
import { PageHeader } from '../components/PageHeader';
import { useNotification } from '../contexts/NotificationContext';
import { usePagedData } from '../hooks/usePagedData';
import type { Employee, Role, User, UserQuery } from '../types/api';
import { formatDateTime } from '../utils/format';

interface UserForm {
  userName: string;
  email: string;
  password: string;
  roleId: number | '';
  employeeId: number | '';
  isActive: boolean;
}

export function UsersPage() {
  const { notifySuccess, notifyError } = useNotification();
  const [roles, setRoles] = useState<Role[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [editing, setEditing] = useState<User | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [userToDelete, setUserToDelete] = useState<User | null>(null);
  const [resetTarget, setResetTarget] = useState<User | null>(null);
  const [newPassword, setNewPassword] = useState('');

  const fetcher = useCallback((query: UserQuery) => usersApi.getPaged(query), []);

  const { data, isLoading, query, setQuery, reload } = usePagedData<User, UserQuery>(fetcher, {
    page: 1,
    pageSize: 20,
    sortBy: 'userName',
  });

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<UserForm>();

  useEffect(() => {
    usersApi.getRoles().then(setRoles).catch(notifyError);
    employeesApi.lookup().then(setEmployees).catch(notifyError);
  }, [notifyError]);

  function openCreate() {
    setEditing(null);
    reset({ userName: '', email: '', password: '', roleId: '', employeeId: '', isActive: true });
    setIsFormOpen(true);
  }

  function openEdit(user: User) {
    setEditing(user);
    reset({
      userName: user.userName,
      email: user.email,
      password: '',
      roleId: user.roleId,
      employeeId: user.employeeId ?? '',
      isActive: user.isActive,
    });
    setIsFormOpen(true);
  }

  async function onSubmit(form: UserForm) {
    try {
      if (editing) {
        await usersApi.update(editing.id, {
          email: form.email,
          roleId: Number(form.roleId),
          employeeId: form.employeeId ? Number(form.employeeId) : undefined,
          isActive: Boolean(form.isActive),
        });
        notifySuccess('Учётная запись обновлена.');
      } else {
        await usersApi.create({
          userName: form.userName,
          email: form.email,
          password: form.password,
          roleId: Number(form.roleId),
          employeeId: form.employeeId ? Number(form.employeeId) : undefined,
        });
        notifySuccess('Учётная запись создана.');
      }

      setIsFormOpen(false);
      reload();
    } catch (error) {
      const fieldErrors = getFieldErrors(error);
      let handled = false;

      for (const [field, message] of Object.entries(fieldErrors)) {
        setError(field as keyof UserForm, { message });
        handled = true;
      }

      if (!handled) {
        notifyError(error);
      }
    }
  }

  const columns: Column<User>[] = [
    { id: 'userName', label: 'Пользователь', sortable: true, render: (u) => u.userName },
    { id: 'email', label: 'E-mail', sortable: true, render: (u) => u.email },
    { id: 'employeeName', label: 'Сотрудник', render: (u) => u.employeeName ?? '—' },
    { id: 'role', label: 'Роль', sortable: true, render: (u) => u.roleDisplayName },
    {
      id: 'lastLoginAt',
      label: 'Последний вход',
      sortable: true,
      width: 150,
      render: (u) => formatDateTime(u.lastLoginAt),
    },
    {
      id: 'status',
      label: 'Состояние',
      width: 140,
      render: (u) => (
        <Stack direction="row" spacing={0.5}>
          {u.isActive ? (
            <Chip label="Активна" size="small" color="success" />
          ) : (
            <Chip label="Отключена" size="small" />
          )}
          {u.isLockedOut ? <Chip label="Блок." size="small" color="error" /> : null}
        </Stack>
      ),
    },
    {
      id: 'actions',
      label: '',
      align: 'right',
      width: 170,
      render: (u) => (
        <Stack direction="row" spacing={0.5} justifyContent="flex-end" onClick={(e) => e.stopPropagation()}>
          <Tooltip title="Изменить">
            <IconButton size="small" onClick={() => openEdit(u)}>
              <EditIcon fontSize="small" />
            </IconButton>
          </Tooltip>

          <Tooltip title="Сбросить пароль">
            <IconButton
              size="small"
              onClick={() => {
                setResetTarget(u);
                setNewPassword('');
              }}
            >
              <PasswordIcon fontSize="small" />
            </IconButton>
          </Tooltip>

          {u.isLockedOut ? (
            <Tooltip title="Снять блокировку">
              <IconButton
                size="small"
                color="warning"
                onClick={async () => {
                  try {
                    await usersApi.unlock(u.id);
                    notifySuccess('Блокировка снята.');
                    reload();
                  } catch (error) {
                    notifyError(error);
                  }
                }}
              >
                <LockOpenIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          ) : null}

          <Tooltip title="Удалить">
            <IconButton size="small" color="error" onClick={() => setUserToDelete(u)}>
              <DeleteIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        </Stack>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Пользователи"
        subtitle="Учётные записи и права доступа"
        actions={
          <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>
            Добавить пользователя
          </Button>
        }
      />

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} md={5}>
              <TextField
                label="Поиск (имя, e-mail, фамилия)"
                fullWidth
                value={query.search ?? ''}
                onChange={(e) => setQuery({ search: e.target.value || undefined })}
              />
            </Grid>

            <Grid item xs={12} sm={6} md={4}>
              <TextField
                select
                label="Роль"
                fullWidth
                value={query.roleId ?? ''}
                onChange={(e) => setQuery({ roleId: e.target.value ? Number(e.target.value) : undefined })}
              >
                <MenuItem value="">Все роли</MenuItem>
                {roles.map((r) => (
                  <MenuItem key={r.id} value={r.id}>
                    {r.displayName}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

            <Grid item xs={12} sm={6} md={3}>
              <TextField
                select
                label="Состояние"
                fullWidth
                value={query.isActive === undefined ? '' : String(query.isActive)}
                onChange={(e) =>
                  setQuery({ isActive: e.target.value === '' ? undefined : e.target.value === 'true' })
                }
              >
                <MenuItem value="">Все</MenuItem>
                <MenuItem value="true">Активные</MenuItem>
                <MenuItem value="false">Отключённые</MenuItem>
              </TextField>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      <DataTable
        columns={columns}
        data={data}
        isLoading={isLoading}
        sortBy={query.sortBy}
        sortDescending={query.sortDescending}
        onSortChange={(sortBy, sortDescending) => setQuery({ sortBy, sortDescending })}
        onPageChange={(page) => setQuery({ page })}
        onPageSizeChange={(pageSize) => setQuery({ pageSize })}
        getRowKey={(u) => u.id}
        emptyMessage="Пользователи не найдены."
      />

      <Dialog open={isFormOpen} onClose={() => setIsFormOpen(false)} maxWidth="sm" fullWidth>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <DialogTitle>{editing ? 'Изменение учётной записи' : 'Новая учётная запись'}</DialogTitle>

          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              <TextField
                label="Имя пользователя"
                fullWidth
                required
                // The login name is the identity used across the audit trail;
                // changing it would break that continuity.
                disabled={editing !== null}
                helperText={
                  errors.userName?.message ?? (editing ? 'Имя пользователя изменить нельзя.' : undefined)
                }
                error={Boolean(errors.userName)}
                {...register('userName', { required: 'Укажите имя пользователя.' })}
              />

              <TextField
                label="E-mail"
                type="email"
                fullWidth
                required
                error={Boolean(errors.email)}
                helperText={errors.email?.message}
                {...register('email', { required: 'Укажите e-mail.' })}
              />

              {!editing ? (
                <TextField
                  label="Пароль"
                  type="password"
                  fullWidth
                  required
                  error={Boolean(errors.password)}
                  helperText={
                    errors.password?.message ??
                    'Не менее 8 символов, минимум одна цифра, заглавная и строчная буква.'
                  }
                  {...register('password', { required: 'Укажите пароль.', minLength: { value: 8, message: 'Не менее 8 символов.' } })}
                />
              ) : null}

              <TextField
                select
                label="Роль"
                fullWidth
                required
                defaultValue=""
                error={Boolean(errors.roleId)}
                helperText={errors.roleId?.message}
                {...register('roleId', { required: 'Выберите роль.' })}
              >
                {roles.map((r) => (
                  <MenuItem key={r.id} value={r.id}>
                    {r.displayName}
                  </MenuItem>
                ))}
              </TextField>

              <TextField
                select
                label="Сотрудник"
                fullWidth
                defaultValue=""
                error={Boolean(errors.employeeId)}
                helperText={
                  errors.employeeId?.message ??
                  'Обязателен для контролёров ОТК: без него нельзя подписывать акты проверки.'
                }
                {...register('employeeId')}
              >
                <MenuItem value="">Не связан</MenuItem>
                {employees.map((e) => (
                  <MenuItem key={e.id} value={e.id}>
                    {e.shortName} — {e.position} ({e.departmentName})
                  </MenuItem>
                ))}
              </TextField>

              {editing ? (
                <TextField select label="Состояние" fullWidth defaultValue={true} {...register('isActive')}>
                  <MenuItem value="true">Активна</MenuItem>
                  <MenuItem value="false">Отключена</MenuItem>
                </TextField>
              ) : null}
            </Stack>
          </DialogContent>

          <DialogActions>
            <Button onClick={() => setIsFormOpen(false)}>Отмена</Button>
            <Button type="submit" variant="contained" disabled={isSubmitting}>
              {isSubmitting ? 'Сохранение…' : 'Сохранить'}
            </Button>
          </DialogActions>
        </form>
      </Dialog>

      <Dialog open={resetTarget !== null} onClose={() => setResetTarget(null)} maxWidth="xs" fullWidth>
        <DialogTitle>Сброс пароля</DialogTitle>

        <DialogContent>
          <TextField
            label="Новый пароль"
            type="password"
            fullWidth
            sx={{ mt: 1 }}
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            helperText={`Пароль для «${resetTarget?.userName}». Все сессии этой записи будут завершены.`}
          />
        </DialogContent>

        <DialogActions>
          <Button onClick={() => setResetTarget(null)}>Отмена</Button>
          <Button
            variant="contained"
            disabled={newPassword.length < 8}
            onClick={async () => {
              if (!resetTarget) {
                return;
              }

              try {
                await usersApi.resetPassword(resetTarget.id, newPassword);
                notifySuccess('Пароль сброшен.');
                setResetTarget(null);
              } catch (error) {
                notifyError(error);
              }
            }}
          >
            Сбросить
          </Button>
        </DialogActions>
      </Dialog>

      <ConfirmDialog
        open={userToDelete !== null}
        title="Удалить учётную запись?"
        message={`Учётная запись «${userToDelete?.userName}» будет удалена, все её сессии завершены. Записи журнала действий сохранятся.`}
        confirmLabel="Удалить"
        destructive
        onConfirm={async () => {
          if (!userToDelete) {
            return;
          }

          try {
            await usersApi.remove(userToDelete.id);
            notifySuccess('Учётная запись удалена.');
            reload();
          } catch (error) {
            notifyError(error);
          }
        }}
        onClose={() => setUserToDelete(null)}
      />
    </>
  );
}
