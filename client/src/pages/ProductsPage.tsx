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
  Archive as ArchiveIcon,
  Delete as DeleteIcon,
  Edit as EditIcon,
  Unarchive as UnarchiveIcon,
} from '@mui/icons-material';
import { categoriesApi, productsApi } from '../api/endpoints';
import { getFieldErrors } from '../api/client';
import { DataTable, type Column } from '../components/DataTable';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { PageHeader } from '../components/PageHeader';
import { useAuth } from '../contexts/AuthContext';
import { useNotification } from '../contexts/NotificationContext';
import { usePagedData } from '../hooks/usePagedData';
import type { Product, ProductCategory, ProductQuery } from '../types/api';

interface ProductForm {
  code: string;
  name: string;
  categoryId: number | '';
  description: string;
  drawingNumber: string;
  unitOfMeasure: string;
}

const emptyForm: ProductForm = {
  code: '',
  name: '',
  categoryId: '',
  description: '',
  drawingNumber: '',
  unitOfMeasure: 'шт.',
};

export function ProductsPage() {
  const { hasRole } = useAuth();
  const { notifySuccess, notifyError } = useNotification();
  const [categories, setCategories] = useState<ProductCategory[]>([]);
  const [editing, setEditing] = useState<Product | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [confirm, setConfirm] = useState<{ product: Product; action: 'delete' | 'archive' | 'restore' } | null>(
    null,
  );

  const canEdit = hasRole('Administrator', 'ProductionManager');
  const canDelete = hasRole('Administrator');

  const fetcher = useCallback((query: ProductQuery) => productsApi.getPaged(query), []);

  const { data, isLoading, query, setQuery, reload } = usePagedData<Product, ProductQuery>(fetcher, {
    page: 1,
    pageSize: 20,
    sortBy: 'code',
  });

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<ProductForm>({ defaultValues: emptyForm });

  useEffect(() => {
    categoriesApi.lookup().then(setCategories).catch(notifyError);
  }, [notifyError]);

  function openCreate() {
    setEditing(null);
    reset(emptyForm);
    setIsFormOpen(true);
  }

  function openEdit(product: Product) {
    setEditing(product);
    reset({
      code: product.code,
      name: product.name,
      categoryId: product.categoryId,
      description: product.description ?? '',
      drawingNumber: product.drawingNumber ?? '',
      unitOfMeasure: product.unitOfMeasure,
    });
    setIsFormOpen(true);
  }

  async function onSubmit(form: ProductForm) {
    const payload = {
      code: form.code,
      name: form.name,
      categoryId: Number(form.categoryId),
      description: form.description || undefined,
      drawingNumber: form.drawingNumber || undefined,
      unitOfMeasure: form.unitOfMeasure,
    };

    try {
      if (editing) {
        await productsApi.update(editing.id, payload);
        notifySuccess('Изделие обновлено.');
      } else {
        await productsApi.create(payload);
        notifySuccess('Изделие создано.');
      }

      setIsFormOpen(false);
      reload();
    } catch (error) {
      // Server-side validation (duplicate code, unknown category) is mapped back
      // onto the offending field rather than shown as a detached toast.
      const fieldErrors = getFieldErrors(error);
      let handled = false;

      for (const [field, message] of Object.entries(fieldErrors)) {
        if (field in emptyForm) {
          setError(field as keyof ProductForm, { message });
          handled = true;
        }
      }

      if (!handled) {
        notifyError(error);
      }
    }
  }

  async function runConfirmedAction() {
    if (!confirm) {
      return;
    }

    const { product, action } = confirm;

    try {
      if (action === 'delete') {
        await productsApi.remove(product.id);
        notifySuccess('Изделие удалено.');
      } else if (action === 'archive') {
        await productsApi.archive(product.id);
        notifySuccess('Изделие переведено в архив.');
      } else {
        await productsApi.restore(product.id);
        notifySuccess('Изделие возвращено из архива.');
      }

      reload();
    } catch (error) {
      notifyError(error);
    }
  }

  const columns: Column<Product>[] = [
    { id: 'code', label: 'Код', sortable: true, width: 180, render: (p) => p.code },
    { id: 'name', label: 'Наименование', sortable: true, render: (p) => p.name },
    { id: 'category', label: 'Категория', sortable: true, render: (p) => p.categoryName },
    {
      id: 'drawingNumber',
      label: 'Чертёж',
      sortable: true,
      render: (p) => p.drawingNumber ?? '—',
    },
    { id: 'batchCount', label: 'Партий', align: 'right', width: 90, render: (p) => p.batchCount },
    {
      id: 'isArchived',
      label: 'Статус',
      width: 120,
      render: (p) =>
        p.isArchived ? <Chip label="В архиве" size="small" /> : <Chip label="Активно" size="small" color="success" />,
    },
    {
      id: 'actions',
      label: '',
      align: 'right',
      width: 140,
      render: (p) => (
        // stopPropagation so an action button does not also trigger the row click.
        <Stack direction="row" spacing={0.5} justifyContent="flex-end" onClick={(e) => e.stopPropagation()}>
          {canEdit ? (
            <Tooltip title="Изменить">
              <IconButton size="small" onClick={() => openEdit(p)}>
                <EditIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          ) : null}

          {canEdit ? (
            <Tooltip title={p.isArchived ? 'Вернуть из архива' : 'В архив'}>
              <IconButton
                size="small"
                onClick={() => setConfirm({ product: p, action: p.isArchived ? 'restore' : 'archive' })}
              >
                {p.isArchived ? <UnarchiveIcon fontSize="small" /> : <ArchiveIcon fontSize="small" />}
              </IconButton>
            </Tooltip>
          ) : null}

          {canDelete ? (
            <Tooltip title="Удалить">
              <IconButton size="small" color="error" onClick={() => setConfirm({ product: p, action: 'delete' })}>
                <DeleteIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          ) : null}
        </Stack>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Изделия"
        subtitle="Справочник изделий, металлоконструкций и узлов"
        actions={
          canEdit ? (
            <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>
              Добавить изделие
            </Button>
          ) : null
        }
      />

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} md={5}>
              <TextField
                label="Поиск по наименованию, коду или чертежу"
                fullWidth
                value={query.search ?? ''}
                onChange={(e) => setQuery({ search: e.target.value || undefined })}
              />
            </Grid>

            <Grid item xs={12} sm={6} md={4}>
              <TextField
                select
                label="Категория"
                fullWidth
                value={query.categoryId ?? ''}
                onChange={(e) => setQuery({ categoryId: e.target.value ? Number(e.target.value) : undefined })}
              >
                <MenuItem value="">Все категории</MenuItem>
                {categories.map((c) => (
                  <MenuItem key={c.id} value={c.id}>
                    {c.name}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

            <Grid item xs={12} sm={6} md={3}>
              <TextField
                select
                label="Состояние"
                fullWidth
                value={query.isArchived === undefined ? '' : String(query.isArchived)}
                onChange={(e) =>
                  setQuery({ isArchived: e.target.value === '' ? undefined : e.target.value === 'true' })
                }
              >
                <MenuItem value="">Все</MenuItem>
                <MenuItem value="false">Активные</MenuItem>
                <MenuItem value="true">В архиве</MenuItem>
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
        getRowKey={(p) => p.id}
        emptyMessage="Изделия не найдены."
      />

      <Dialog open={isFormOpen} onClose={() => setIsFormOpen(false)} maxWidth="sm" fullWidth>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <DialogTitle>{editing ? 'Изменение изделия' : 'Новое изделие'}</DialogTitle>

          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              <TextField
                label="Код (артикул)"
                fullWidth
                required
                error={Boolean(errors.code)}
                helperText={errors.code?.message}
                {...register('code', { required: 'Укажите код изделия.' })}
              />

              <TextField
                label="Наименование"
                fullWidth
                required
                error={Boolean(errors.name)}
                helperText={errors.name?.message}
                {...register('name', { required: 'Укажите наименование.' })}
              />

              <TextField
                select
                label="Категория"
                fullWidth
                required
                defaultValue=""
                error={Boolean(errors.categoryId)}
                helperText={errors.categoryId?.message}
                {...register('categoryId', { required: 'Выберите категорию.' })}
              >
                {categories.map((c) => (
                  <MenuItem key={c.id} value={c.id}>
                    {c.name}
                  </MenuItem>
                ))}
              </TextField>

              <TextField
                label="Обозначение чертежа"
                fullWidth
                error={Boolean(errors.drawingNumber)}
                helperText={errors.drawingNumber?.message}
                {...register('drawingNumber')}
              />

              <TextField
                label="Единица измерения"
                fullWidth
                required
                error={Boolean(errors.unitOfMeasure)}
                helperText={errors.unitOfMeasure?.message}
                {...register('unitOfMeasure', { required: 'Укажите единицу измерения.' })}
              />

              <TextField
                label="Описание"
                fullWidth
                multiline
                rows={3}
                error={Boolean(errors.description)}
                helperText={errors.description?.message}
                {...register('description')}
              />
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

      <ConfirmDialog
        open={confirm !== null}
        title={
          confirm?.action === 'delete'
            ? 'Удалить изделие?'
            : confirm?.action === 'archive'
              ? 'Перевести в архив?'
              : 'Вернуть из архива?'
        }
        message={
          confirm?.action === 'delete'
            ? `Изделие «${confirm.product.name}» будет удалено. Если по нему есть партии, удаление будет отклонено.`
            : confirm?.action === 'archive'
              ? `Изделие «${confirm?.product.name}» сохранит всю историю, но станет недоступно для новых партий.`
              : `Изделие «${confirm?.product.name}» снова станет доступно для новых партий.`
        }
        confirmLabel={confirm?.action === 'delete' ? 'Удалить' : 'Подтвердить'}
        destructive={confirm?.action === 'delete'}
        onConfirm={runConfirmedAction}
        onClose={() => setConfirm(null)}
      />
    </>
  );
}
