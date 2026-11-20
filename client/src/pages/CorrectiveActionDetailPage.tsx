import { useCallback, useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Alert,
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
  MenuItem,
  Stack,
  Step,
  StepContent,
  StepLabel,
  Stepper,
  TextField,
  Typography,
} from '@mui/material';
import { correctiveActionsApi, employeesApi } from '../api/endpoints';
import { PageHeader } from '../components/PageHeader';
import { CorrectiveActionStatusChip, DefectSeverityChip } from '../components/StatusChip';
import { useNotification } from '../contexts/NotificationContext';
import type { CorrectiveAction, CorrectiveActionStatus, Employee } from '../types/api';
import { formatDate, formatDateTime } from '../utils/format';
import { correctiveActionStatusLabels } from '../utils/labels';

export function CorrectiveActionDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { notifySuccess, notifyError } = useNotification();

  const [action, setAction] = useState<CorrectiveAction | null>(null);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [statusDialog, setStatusDialog] = useState<CorrectiveActionStatus | null>(null);
  const [statusComment, setStatusComment] = useState('');
  const [isAssignOpen, setIsAssignOpen] = useState(false);
  const [assignee, setAssignee] = useState<number | ''>('');
  const [isBusy, setIsBusy] = useState(false);

  const load = useCallback(async () => {
    if (!id) {
      return;
    }

    try {
      setAction(await correctiveActionsApi.getById(Number(id)));
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

  async function changeStatus() {
    if (!action || !statusDialog) {
      return;
    }

    setIsBusy(true);

    try {
      await correctiveActionsApi.changeStatus(action.id, statusDialog, statusComment || undefined);
      notifySuccess(`Статус изменён на «${correctiveActionStatusLabels[statusDialog]}».`);
      setStatusDialog(null);
      setStatusComment('');
      await load();
    } catch (error) {
      notifyError(error);
    } finally {
      setIsBusy(false);
    }
  }

  async function assign() {
    if (!action || assignee === '') {
      return;
    }

    setIsBusy(true);

    try {
      await correctiveActionsApi.assign(action.id, Number(assignee));
      notifySuccess('Ответственный назначен.');
      setIsAssignOpen(false);
      await load();
    } catch (error) {
      notifyError(error);
    } finally {
      setIsBusy(false);
    }
  }

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!action) {
    return <Typography color="text.secondary">Корректирующее действие не найдено.</Typography>;
  }

  return (
    <>
      <PageHeader
        title={`Корректирующее действие №${action.id}`}
        subtitle={`Партия ${action.batchNumber} · ${action.productName} · ${action.departmentName}`}
        actions={
          <>
            {action.status !== 'Closed' ? (
              <Button variant="outlined" onClick={() => setIsAssignOpen(true)}>
                {action.assignedToId ? 'Сменить ответственного' : 'Назначить ответственного'}
              </Button>
            ) : null}

            {/* Only the transitions the domain will accept are offered — the list
                comes from the server, not from a duplicate of the state machine. */}
            {action.allowedNextStatuses?.map((status) => (
              <Button
                key={status}
                variant="contained"
                color={status === 'Closed' ? 'success' : 'primary'}
                onClick={() => setStatusDialog(status)}
              >
                {correctiveActionStatusLabels[status]}
              </Button>
            ))}
          </>
        }
      />

      {action.isOverdue ? (
        <Alert severity="error" sx={{ mb: 2 }}>
          Срок исполнения ({formatDate(action.dueDate)}) истёк, действие не закрыто.
        </Alert>
      ) : null}

      <Grid container spacing={2}>
        <Grid item xs={12} md={7}>
          <Card sx={{ mb: 2 }}>
            <CardContent>
              <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
                <Typography variant="h6">Мероприятие</Typography>
                <CorrectiveActionStatusChip status={action.status} size="medium" />
              </Stack>

              <Typography variant="body1" sx={{ mb: 2 }}>
                {action.description}
              </Typography>

              <Divider sx={{ my: 2 }} />

              <Grid container spacing={2}>
                <Grid item xs={6} sm={4}>
                  <Typography variant="caption" color="text.secondary" display="block">
                    Ответственный
                  </Typography>
                  <Typography variant="body2">{action.assignedToName ?? 'Не назначен'}</Typography>
                </Grid>

                <Grid item xs={6} sm={4}>
                  <Typography variant="caption" color="text.secondary" display="block">
                    Срок исполнения
                  </Typography>
                  <Typography variant="body2">{formatDate(action.dueDate)}</Typography>
                </Grid>

                <Grid item xs={6} sm={4}>
                  <Typography variant="caption" color="text.secondary" display="block">
                    Создано
                  </Typography>
                  <Typography variant="body2">{formatDate(action.createdAt)}</Typography>
                </Grid>

                {action.verifiedByName ? (
                  <Grid item xs={6} sm={4}>
                    <Typography variant="caption" color="text.secondary" display="block">
                      Принял ОТК
                    </Typography>
                    <Typography variant="body2">{action.verifiedByName}</Typography>
                  </Grid>
                ) : null}

                {action.closedAt ? (
                  <Grid item xs={6} sm={4}>
                    <Typography variant="caption" color="text.secondary" display="block">
                      Закрыто
                    </Typography>
                    <Typography variant="body2">{formatDate(action.closedAt)}</Typography>
                  </Grid>
                ) : null}

                {action.resolutionNotes ? (
                  <Grid item xs={12}>
                    <Typography variant="caption" color="text.secondary" display="block">
                      Выполненные работы
                    </Typography>
                    <Typography variant="body2">{action.resolutionNotes}</Typography>
                  </Grid>
                ) : null}
              </Grid>
            </CardContent>
          </Card>

          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Дефект
              </Typography>

              <Stack direction="row" justifyContent="space-between" spacing={1} sx={{ mb: 1 }}>
                <Typography variant="subtitle2">{action.defectTypeName}</Typography>
                <DefectSeverityChip severity={action.defectSeverity} />
              </Stack>

              <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                {action.defectDescription}
              </Typography>

              <Button size="small" onClick={() => navigate(`/defects/${action.defectId}`)}>
                Открыть карточку дефекта
              </Button>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={5}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                История изменений
              </Typography>

              {!action.history || action.history.length === 0 ? (
                <Typography color="text.secondary">История пуста.</Typography>
              ) : (
                // Oldest first reads as a timeline; the API returns newest first.
                <Stepper orientation="vertical" activeStep={-1} sx={{ mt: 1 }}>
                  {[...action.history]
                    .sort((a, b) => a.changedAt.localeCompare(b.changedAt))
                    .map((h) => (
                      <Step key={h.id} active expanded>
                        <StepLabel
                          optional={
                            <Typography variant="caption" color="text.secondary">
                              {formatDateTime(h.changedAt)} · {h.changedByUserName}
                            </Typography>
                          }
                        >
                          <Stack direction="row" spacing={1} alignItems="center">
                            {h.fromStatus ? (
                              <>
                                <Chip label={correctiveActionStatusLabels[h.fromStatus]} size="small" />
                                <Typography variant="caption">→</Typography>
                              </>
                            ) : null}
                            <CorrectiveActionStatusChip status={h.toStatus} />
                          </Stack>
                        </StepLabel>

                        {h.comment ? (
                          <StepContent>
                            <Typography variant="body2" color="text.secondary">
                              {h.comment}
                            </Typography>
                          </StepContent>
                        ) : null}
                      </Step>
                    ))}
                </Stepper>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Dialog open={statusDialog !== null} onClose={() => setStatusDialog(null)} maxWidth="xs" fullWidth>
        <DialogTitle>
          Перевести в статус «{statusDialog ? correctiveActionStatusLabels[statusDialog] : ''}»?
        </DialogTitle>

        <DialogContent>
          {statusDialog === 'Verified' ? (
            <Alert severity="info" sx={{ mb: 2 }}>
              Приёмку устранения может выполнить только контролёр ОТК.
            </Alert>
          ) : null}

          <TextField
            label="Комментарий"
            fullWidth
            multiline
            rows={3}
            sx={{ mt: 1 }}
            value={statusComment}
            onChange={(e) => setStatusComment(e.target.value)}
          />
        </DialogContent>

        <DialogActions>
          <Button onClick={() => setStatusDialog(null)} disabled={isBusy}>
            Отмена
          </Button>
          <Button variant="contained" onClick={() => void changeStatus()} disabled={isBusy}>
            {isBusy ? 'Сохранение…' : 'Подтвердить'}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={isAssignOpen} onClose={() => setIsAssignOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle>Назначение ответственного</DialogTitle>

        <DialogContent>
          <TextField
            select
            label="Сотрудник"
            fullWidth
            sx={{ mt: 1 }}
            value={assignee}
            onChange={(e) => setAssignee(e.target.value ? Number(e.target.value) : '')}
          >
            {employees.map((e) => (
              <MenuItem key={e.id} value={e.id}>
                {e.shortName} — {e.position} ({e.departmentName})
              </MenuItem>
            ))}
          </TextField>
        </DialogContent>

        <DialogActions>
          <Button onClick={() => setIsAssignOpen(false)} disabled={isBusy}>
            Отмена
          </Button>
          <Button variant="contained" onClick={() => void assign()} disabled={isBusy || assignee === ''}>
            {isBusy ? 'Сохранение…' : 'Назначить'}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
