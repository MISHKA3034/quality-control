import { useCallback, useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import {
  Button,
  Card,
  CardContent,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  MenuItem,
  Stack,
  TextField,
} from '@mui/material';
import { Add as AddIcon } from '@mui/icons-material';
import { batchesApi, departmentsApi, employeesApi, productsApi } from '../api/endpoints';
import { getFieldErrors } from '../api/client';
import { DataTable, type Column } from '../components/DataTable';
import { PageHeader } from '../components/PageHeader';
import { BatchStatusChip } from '../components/StatusChip';
import { useAuth } from '../contexts/AuthContext';
import { useNotification } from '../contexts/NotificationContext';
import { usePagedData } from '../hooks/usePagedData';
import type {
  Department,
  Employee,
  Product,
  ProductionBatch,
  ProductionBatchQuery,
} from '../types/api';
import { formatDate, formatNumber, toInputDate } from '../utils/format';
import { batchStatusLabels } from '../utils/labels';

interface BatchForm {
  batchNumber: string;
  productId: number | '';
  manufactureDate: string;
  departmentId: number | '';
  foremanId: number | '';
  quantity: number | '';
  notes: string;
}

export function BatchesPage() {
  const { hasRole } = useAuth();
  const { notifySuccess, notifyError } = useNotification();
  const [products, setProducts] = useState<Product[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [isFormOpen, setIsFormOpen] = useState(false);

  const canEdit = hasRole('Administrator', 'WorkshopManager', 'ProductionManager');

  const fetcher = useCallback((query: ProductionBatchQuery) => batchesApi.getPaged(query), []);

  const { data, isLoading, query, setQuery, reload } = usePagedData<ProductionBatch, ProductionBatchQuery>(
    fetcher,
    { page: 1, pageSize: 20, sortBy: 'manufactureDate', sortDescending: true },
  );

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<BatchForm>();

  useEffect(() => {
    // Only active products can start a batch, so archived ones are not offered.
    productsApi
      .getPaged({ pageSize: 200, isArchived: false })
      .then((r) => setProducts(r.items))
      .catch(notifyError);

    departmentsApi.lookup().then(setDepartments).catch(notifyError);
    employeesApi.lookup().then(setEmployees).catch(notifyError);
  }, [notifyError]);

  function openCreate() {
    reset({
      batchNumber: '',
      productId: '',
      manufactureDate: toInputDate(new Date()),
      departmentId: '',
      foremanId: '',
      quantity: '',
      notes: '',
    });
    setIsFormOpen(true);
  }

  async function onSubmit(form: BatchForm) {
    try {
      await batchesApi.create({
        batchNumber: form.batchNumber,
        productId: Number(form.productId),
        manufactureDate: new Date(form.manufactureDate).toISOString(),
        departmentId: Number(form.departmentId),
        foremanId: Number(form.foremanId),
        quantity: Number(form.quantity),
        notes: form.notes || undefined,
      });

      notifySuccess('Партия зарегистрирована.');
      setIsFormOpen(false);
      reload();
    } catch (error) {
      const fieldErrors = getFieldErrors(error);
      let handled = false;

      for (const [field, message] of Object.entries(fieldErrors)) {
        setError(field as keyof BatchForm, { message });
        handled = true;
      }

      if (!handled) {
        notifyError(error);
      }
    }
  }

  const columns: Column<ProductionBatch>[] = [
    { id: 'batchNumber', label: 'Номер партии', sortable: true, width: 160, render: (b) => b.batchNumber },
    {
      id: 'manufactureDate',
      label: 'Дата изготовления',
      sortable: true,
      width: 150,
      render: (b) => formatDate(b.manufactureDate),
    },
    { id: 'product', label: 'Изделие', sortable: true, render: (b) => b.productName },
    { id: 'department', label: 'Участок', sortable: true, render: (b) => b.departmentName },
    { id: 'foreman', label: 'Мастер', render: (b) => b.foremanName },
    {
      id: 'quantity',
      label: 'Кол-во',
      sortable: true,
      align: 'right',
      width: 90,
      render: (b) => formatNumber(b.quantity),
    },
    {
      id: 'status',
      label: 'Статус',
      sortable: true,
      width: 170,
      render: (b) => <BatchStatusChip status={b.status} />,
    },
    { id: 'inspectionCount', label: 'Проверок', align: 'right', width: 90, render: (b) => b.inspectionCount },
  ];

  return (
    <>
      <PageHeader
        title="Производственные партии"
        subtitle="Учёт партий и их движение по маршруту контроля"
        actions={
          canEdit ? (
            <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>
              Зарегистрировать партию
            </Button>
          ) : null
        }
      />

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} md={3}>
              <TextField
                label="Поиск (номер, изделие)"
                fullWidth
                value={query.search ?? ''}
                onChange={(e) => setQuery({ search: e.target.value || undefined })}
              />
            </Grid>

            <Grid item xs={12} sm={6} md={2}>
              <TextField
                select
                label="Участок"
                fullWidth
                value={query.departmentId ?? ''}
                onChange={(e) => setQuery({ departmentId: e.target.value ? Number(e.target.value) : undefined })}
              >
                <MenuItem value="">Все</MenuItem>
                {departments.map((d) => (
                  <MenuItem key={d.id} value={d.id}>
                    {d.name}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

            <Grid item xs={12} sm={6} md={2}>
              <TextField
                select
                label="Статус"
                fullWidth
                value={query.status ?? ''}
                onChange={(e) => setQuery({ status: (e.target.value || undefined) as never })}
              >
                <MenuItem value="">Все</MenuItem>
                {Object.entries(batchStatusLabels).map(([value, label]) => (
                  <MenuItem key={value} value={value}>
                    {label}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

            <Grid item xs={12} sm={6} md={2.5}>
              <TextField
                type="date"
                label="Дата с"
                fullWidth
                InputLabelProps={{ shrink: true }}
                value={query.dateFrom ? toInputDate(query.dateFrom) : ''}
                onChange={(e) =>
                  setQuery({ dateFrom: e.target.value ? new Date(e.target.value).toISOString() : undefined })
                }
              />
            </Grid>

            <Grid item xs={12} sm={6} md={2.5}>
              <TextField
                type="date"
                label="Дата по"
                fullWidth
                InputLabelProps={{ shrink: true }}
                value={query.dateTo ? toInputDate(query.dateTo) : ''}
                onChange={(e) =>
                  setQuery({ dateTo: e.target.value ? new Date(e.target.value).toISOString() : undefined })
                }
              />
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
        getRowKey={(b) => b.id}
        emptyMessage="Партии не найдены."
      />

      <Dialog open={isFormOpen} onClose={() => setIsFormOpen(false)} maxWidth="sm" fullWidth>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <DialogTitle>Регистрация производственной партии</DialogTitle>

          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              <TextField
                label="Номер партии"
                fullWidth
                required
                error={Boolean(errors.batchNumber)}
                helperText={errors.batchNumber?.message}
                {...register('batchNumber', { required: 'Укажите номер партии.' })}
              />

              <TextField
                select
                label="Изделие"
                fullWidth
                required
                defaultValue=""
                error={Boolean(errors.productId)}
                helperText={errors.productId?.message}
                {...register('productId', { required: 'Выберите изделие.' })}
              >
                {products.map((p) => (
                  <MenuItem key={p.id} value={p.id}>
                    {p.code} — {p.name}
                  </MenuItem>
                ))}
              </TextField>

              <TextField
                type="date"
                label="Дата изготовления"
                fullWidth
                required
                InputLabelProps={{ shrink: true }}
                error={Boolean(errors.manufactureDate)}
                helperText={errors.manufactureDate?.message}
                {...register('manufactureDate', { required: 'Укажите дату изготовления.' })}
              />

              <TextField
                select
                label="Участок"
                fullWidth
                required
                defaultValue=""
                error={Boolean(errors.departmentId)}
                helperText={errors.departmentId?.message}
                {...register('departmentId', { required: 'Выберите участок.' })}
              >
                {departments.map((d) => (
                  <MenuItem key={d.id} value={d.id}>
                    {d.name}
                  </MenuItem>
                ))}
              </TextField>

              <TextField
                select
                label="Ответственный мастер"
                fullWidth
                required
                defaultValue=""
                error={Boolean(errors.foremanId)}
                helperText={errors.foremanId?.message}
                {...register('foremanId', { required: 'Выберите мастера.' })}
              >
                {employees.map((e) => (
                  <MenuItem key={e.id} value={e.id}>
                    {e.shortName} — {e.position}
                  </MenuItem>
                ))}
              </TextField>

              <TextField
                type="number"
                label="Количество изделий"
                fullWidth
                required
                inputProps={{ min: 1 }}
                error={Boolean(errors.quantity)}
                helperText={errors.quantity?.message}
                {...register('quantity', {
                  required: 'Укажите количество.',
                  min: { value: 1, message: 'Количество должно быть больше нуля.' },
                })}
              />

              <TextField label="Примечание" fullWidth multiline rows={2} {...register('notes')} />
            </Stack>
          </DialogContent>

          <DialogActions>
            <Button onClick={() => setIsFormOpen(false)}>Отмена</Button>
            <Button type="submit" variant="contained" disabled={isSubmitting}>
              {isSubmitting ? 'Сохранение…' : 'Зарегистрировать'}
            </Button>
          </DialogActions>
        </form>
      </Dialog>
    </>
  );
}
