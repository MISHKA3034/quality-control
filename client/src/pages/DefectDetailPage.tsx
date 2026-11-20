import { useCallback, useEffect, useRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  Grid,
  IconButton,
  ImageList,
  ImageListItem,
  ImageListItemBar,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import {
  Add as AddIcon,
  Delete as DeleteIcon,
  PhotoCamera as PhotoCameraIcon,
} from '@mui/icons-material';
import { correctiveActionsApi, defectsApi, employeesApi } from '../api/endpoints';
import { apiClient } from '../api/client';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { PageHeader } from '../components/PageHeader';
import { CorrectiveActionStatusChip, DefectSeverityChip } from '../components/StatusChip';
import { useAuth } from '../contexts/AuthContext';
import { useNotification } from '../contexts/NotificationContext';
import type { CorrectiveAction, Defect, DefectPhoto, Employee } from '../types/api';
import { formatDate, formatFileSize, toInputDate } from '../utils/format';

/**
 * Photos are behind an authenticated endpoint, so an <img src> would 401.
 * The blob is fetched with the auth header and shown from an object URL.
 */
function AuthorisedImage({ photo }: { photo: DefectPhoto }) {
  const [src, setSrc] = useState<string | null>(null);

  useEffect(() => {
    let objectUrl: string | null = null;
    let cancelled = false;

    apiClient
      .get<Blob>(photo.url.replace('/api', ''), { responseType: 'blob' })
      .then((response) => {
        if (!cancelled) {
          objectUrl = URL.createObjectURL(response.data);
          setSrc(objectUrl);
        }
      })
      .catch(() => {
        // A broken thumbnail is not worth a toast; the tile just stays empty.
      });

    return () => {
      cancelled = true;

      // Without this the blob stays in memory for the life of the tab.
      if (objectUrl) {
        URL.revokeObjectURL(objectUrl);
      }
    };
  }, [photo.url]);

  if (!src) {
    return (
      <Box
        sx={{
          height: 160,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          bgcolor: 'grey.100',
        }}
      >
        <CircularProgress size={20} />
      </Box>
    );
  }

  return (
    <img
      src={src}
      alt={photo.caption ?? photo.originalFileName}
      loading="lazy"
      style={{ width: '100%', height: 160, objectFit: 'cover', display: 'block' }}
    />
  );
}

export function DefectDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { hasRole } = useAuth();
  const { notifySuccess, notifyError } = useNotification();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [defect, setDefect] = useState<Defect | null>(null);
  const [actions, setActions] = useState<CorrectiveAction[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isUploading, setIsUploading] = useState(false);
  const [photoToDelete, setPhotoToDelete] = useState<DefectPhoto | null>(null);
  const [isActionFormOpen, setIsActionFormOpen] = useState(false);

  const canEditDefect = hasRole('Administrator', 'QualityInspector');
  const canCreateAction = hasRole('Administrator', 'QualityInspector', 'ProductionManager');

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<{ description: string; assignedToId: number | ''; dueDate: string }>();

  const load = useCallback(async () => {
    if (!id) {
      return;
    }

    try {
      const [d, a] = await Promise.all([
        defectsApi.getById(Number(id)),
        correctiveActionsApi.getPaged({ defectId: Number(id), pageSize: 50 }),
      ]);

      setDefect(d);
      setActions(a.items);
    } catch (error) {
      notifyError(error);
    } finally {
      setIsLoading(false);
    }
  }, [id, notifyError]);

  useEffect(() => {
    void load();
    employeesApi.lookup().then(setEmployees).catch(notifyError);
  }, [load, notifyError]);

  async function handleFileSelected(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];

    if (!file || !defect) {
      return;
    }

    setIsUploading(true);

    try {
      await defectsApi.uploadPhoto(defect.id, file);
      notifySuccess('Фотография загружена.');
      await load();
    } catch (error) {
      notifyError(error);
    } finally {
      setIsUploading(false);

      // Resets the input so selecting the same file again still fires onChange.
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  }

  async function handleDeletePhoto() {
    if (!photoToDelete || !defect) {
      return;
    }

    try {
      await defectsApi.deletePhoto(defect.id, photoToDelete.id);
      notifySuccess('Фотография удалена.');
      await load();
    } catch (error) {
      notifyError(error);
    }
  }

  async function onCreateAction(form: { description: string; assignedToId: number | ''; dueDate: string }) {
    if (!defect) {
      return;
    }

    try {
      await correctiveActionsApi.create({
        defectId: defect.id,
        description: form.description,
        assignedToId: form.assignedToId ? Number(form.assignedToId) : undefined,
        dueDate: form.dueDate ? new Date(form.dueDate).toISOString() : undefined,
      });

      notifySuccess('Корректирующее действие создано.');
      setIsActionFormOpen(false);
      await load();
    } catch (error) {
      notifyError(error);
    }
  }

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!defect) {
    return <Typography color="text.secondary">Дефект не найден.</Typography>;
  }

  return (
    <>
      <PageHeader
        title={`Дефект №${defect.id}`}
        subtitle={`${defect.defectTypeName} · партия ${defect.batchNumber} · ${defect.productName}`}
        actions={
          canCreateAction ? (
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => {
                reset({ description: `Устранить: ${defect.description}`, assignedToId: '', dueDate: '' });
                setIsActionFormOpen(true);
              }}
            >
              Создать корректирующее действие
            </Button>
          ) : null
        }
      />

      <Grid container spacing={2}>
        <Grid item xs={12} md={7}>
          <Card sx={{ mb: 2 }}>
            <CardContent>
              <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
                <Typography variant="h6">Описание</Typography>
                <Stack direction="row" spacing={1}>
                  <DefectSeverityChip severity={defect.severity} />
                  {defect.isOpen ? (
                    <Chip label="Открыт" size="small" color="warning" />
                  ) : (
                    <Chip label="Закрыт" size="small" color="success" />
                  )}
                </Stack>
              </Stack>

              <Typography variant="body1" sx={{ mb: 2 }}>
                {defect.description}
              </Typography>

              <Divider sx={{ my: 2 }} />

              <Grid container spacing={2}>
                <Grid item xs={6} sm={4}>
                  <Typography variant="caption" color="text.secondary" display="block">
                    Обнаружен
                  </Typography>
                  <Typography variant="body2">{formatDate(defect.detectedAt)}</Typography>
                </Grid>

                <Grid item xs={6} sm={4}>
                  <Typography variant="caption" color="text.secondary" display="block">
                    Место возникновения
                  </Typography>
                  <Typography variant="body2">{defect.location ?? '—'}</Typography>
                </Grid>

                <Grid item xs={6} sm={4}>
                  <Typography variant="caption" color="text.secondary" display="block">
                    Изделий затронуто
                  </Typography>
                  <Typography variant="body2">{defect.affectedQuantity}</Typography>
                </Grid>

                <Grid item xs={6} sm={4}>
                  <Typography variant="caption" color="text.secondary" display="block">
                    Акт
                  </Typography>
                  <Typography
                    variant="body2"
                    sx={{ cursor: 'pointer', color: 'primary.main' }}
                    onClick={() => navigate(`/inspections/${defect.qualityInspectionId}`)}
                  >
                    {defect.inspectionNumber}
                  </Typography>
                </Grid>

                <Grid item xs={6} sm={4}>
                  <Typography variant="caption" color="text.secondary" display="block">
                    Участок
                  </Typography>
                  <Typography variant="body2">{defect.departmentName}</Typography>
                </Grid>

                {defect.comment ? (
                  <Grid item xs={12}>
                    <Typography variant="caption" color="text.secondary" display="block">
                      Комментарий
                    </Typography>
                    <Typography variant="body2">{defect.comment}</Typography>
                  </Grid>
                ) : null}
              </Grid>
            </CardContent>
          </Card>

          <Card>
            <CardContent>
              <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
                <Typography variant="h6">Фотографии ({defect.photos?.length ?? 0})</Typography>

                {canEditDefect ? (
                  <>
                    <input
                      ref={fileInputRef}
                      type="file"
                      accept="image/jpeg,image/png,image/webp"
                      hidden
                      onChange={(e) => void handleFileSelected(e)}
                    />

                    <Button
                      size="small"
                      startIcon={<PhotoCameraIcon />}
                      disabled={isUploading}
                      onClick={() => fileInputRef.current?.click()}
                    >
                      {isUploading ? 'Загрузка…' : 'Добавить фото'}
                    </Button>
                  </>
                ) : null}
              </Stack>

              {!defect.photos || defect.photos.length === 0 ? (
                <Typography color="text.secondary">Фотографии не приложены.</Typography>
              ) : (
                <ImageList cols={3} gap={8} sx={{ m: 0 }}>
                  {defect.photos.map((photo) => (
                    <ImageListItem key={photo.id}>
                      <AuthorisedImage photo={photo} />

                      <ImageListItemBar
                        title={photo.caption ?? photo.originalFileName}
                        subtitle={formatFileSize(photo.fileSizeBytes)}
                        actionIcon={
                          canEditDefect ? (
                            <IconButton
                              sx={{ color: 'rgba(255,255,255,0.85)' }}
                              onClick={() => setPhotoToDelete(photo)}
                              aria-label="Удалить фотографию"
                            >
                              <DeleteIcon fontSize="small" />
                            </IconButton>
                          ) : undefined
                        }
                      />
                    </ImageListItem>
                  ))}
                </ImageList>
              )}
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={5}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Корректирующие действия ({actions.length})
              </Typography>

              {actions.length === 0 ? (
                <Typography color="text.secondary">
                  Действия не созданы. Дефект считается открытым, пока по нему не будет
                  закрыто хотя бы одно корректирующее действие.
                </Typography>
              ) : (
                <Stack spacing={1.5}>
                  {actions.map((a) => (
                    <Card
                      key={a.id}
                      variant="outlined"
                      sx={{ cursor: 'pointer', '&:hover': { borderColor: 'primary.main' } }}
                      onClick={() => navigate(`/corrective-actions/${a.id}`)}
                    >
                      <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
                        <Stack direction="row" justifyContent="space-between" spacing={1} sx={{ mb: 0.5 }}>
                          <Typography variant="subtitle2">№{a.id}</Typography>
                          <Stack direction="row" spacing={0.5}>
                            {a.isOverdue ? <Chip label="Просрочено" size="small" color="error" /> : null}
                            <CorrectiveActionStatusChip status={a.status} />
                          </Stack>
                        </Stack>

                        <Typography variant="body2" color="text.secondary">
                          {a.description}
                        </Typography>

                        <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.5 }}>
                          {a.assignedToName ? `Ответственный: ${a.assignedToName}` : 'Ответственный не назначен'}
                          {a.dueDate ? ` · срок ${formatDate(a.dueDate)}` : ''}
                        </Typography>
                      </CardContent>
                    </Card>
                  ))}
                </Stack>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Dialog open={isActionFormOpen} onClose={() => setIsActionFormOpen(false)} maxWidth="sm" fullWidth>
        <form onSubmit={handleSubmit(onCreateAction)} noValidate>
          <DialogTitle>Новое корректирующее действие</DialogTitle>

          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              <TextField
                label="Что необходимо выполнить"
                fullWidth
                required
                multiline
                rows={3}
                error={Boolean(errors.description)}
                helperText={errors.description?.message}
                {...register('description', { required: 'Опишите корректирующее действие.' })}
              />

              <TextField select label="Ответственный" fullWidth defaultValue="" {...register('assignedToId')}>
                <MenuItem value="">Назначить позже</MenuItem>
                {employees.map((e) => (
                  <MenuItem key={e.id} value={e.id}>
                    {e.shortName} — {e.position}
                  </MenuItem>
                ))}
              </TextField>

              <TextField
                type="date"
                label="Срок исполнения"
                fullWidth
                InputLabelProps={{ shrink: true }}
                inputProps={{ min: toInputDate(new Date()) }}
                {...register('dueDate')}
              />
            </Stack>
          </DialogContent>

          <DialogActions>
            <Button onClick={() => setIsActionFormOpen(false)}>Отмена</Button>
            <Button type="submit" variant="contained" disabled={isSubmitting}>
              {isSubmitting ? 'Создание…' : 'Создать'}
            </Button>
          </DialogActions>
        </form>
      </Dialog>

      <ConfirmDialog
        open={photoToDelete !== null}
        title="Удалить фотографию?"
        message={`Фотография «${photoToDelete?.caption ?? photoToDelete?.originalFileName}» будет удалена безвозвратно.`}
        confirmLabel="Удалить"
        destructive
        onConfirm={handleDeletePhoto}
        onClose={() => setPhotoToDelete(null)}
      />
    </>
  );
}
