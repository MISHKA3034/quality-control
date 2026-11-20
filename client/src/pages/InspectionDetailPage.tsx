import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Card,
  CardContent,
  CircularProgress,
  Divider,
  Grid,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { inspectionsApi } from '../api/endpoints';
import { PageHeader } from '../components/PageHeader';
import { DefectSeverityChip, InspectionOutcomeChip } from '../components/StatusChip';
import { useNotification } from '../contexts/NotificationContext';
import type { QualityInspection } from '../types/api';
import { formatDate, formatNumber, formatPercent } from '../utils/format';

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary" display="block">
        {label}
      </Typography>
      <Typography variant="body2">{value}</Typography>
    </Box>
  );
}

export function InspectionDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { notifyError } = useNotification();
  const [inspection, setInspection] = useState<QualityInspection | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    if (!id) {
      return;
    }

    inspectionsApi
      .getById(Number(id))
      .then(setInspection)
      .catch(notifyError)
      .finally(() => setIsLoading(false));
  }, [id, notifyError]);

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!inspection) {
    return <Typography color="text.secondary">Акт не найден.</Typography>;
  }

  const defectRate =
    inspection.quantityInspected === 0
      ? 0
      : (inspection.quantityRejected / inspection.quantityInspected) * 100;

  return (
    <>
      <PageHeader
        title={`Акт ${inspection.inspectionNumber}`}
        subtitle={`Партия ${inspection.batchNumber} · ${inspection.productName}`}
      />

      <Grid container spacing={2}>
        <Grid item xs={12} md={8}>
          <Card sx={{ mb: 2 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Сведения об акте
              </Typography>

              <Grid container spacing={2}>
                <Grid item xs={6} sm={4}>
                  <Field label="Дата проверки" value={formatDate(inspection.inspectionDate)} />
                </Grid>
                <Grid item xs={6} sm={4}>
                  <Field label="Контролёр" value={inspection.inspectorName} />
                </Grid>
                <Grid item xs={6} sm={4}>
                  <Field label="Участок" value={inspection.departmentName} />
                </Grid>
                <Grid item xs={6} sm={4}>
                  <Field
                    label="Результат"
                    value={<InspectionOutcomeChip outcome={inspection.outcome} />}
                  />
                </Grid>
                <Grid item xs={6} sm={4}>
                  <Field
                    label="Проверено / принято / брак"
                    value={`${formatNumber(inspection.quantityInspected)} / ${formatNumber(
                      inspection.quantityAccepted,
                    )} / ${formatNumber(inspection.quantityRejected)}`}
                  />
                </Grid>
                <Grid item xs={6} sm={4}>
                  <Field label="Доля брака в выборке" value={formatPercent(defectRate)} />
                </Grid>

                {inspection.comment ? (
                  <Grid item xs={12}>
                    <Divider sx={{ my: 1 }} />
                    <Field label="Комментарий" value={inspection.comment} />
                  </Grid>
                ) : null}
              </Grid>
            </CardContent>
          </Card>

          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Контролируемые параметры
              </Typography>

              {!inspection.results || inspection.results.length === 0 ? (
                <Typography color="text.secondary">Параметры не указаны.</Typography>
              ) : (
                <Box sx={{ overflowX: 'auto' }}>
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell>Параметр</TableCell>
                        <TableCell>Требование</TableCell>
                        <TableCell>Факт</TableCell>
                        <TableCell>Ед.</TableCell>
                        <TableCell>Вердикт</TableCell>
                      </TableRow>
                    </TableHead>

                    <TableBody>
                      {inspection.results.map((r) => (
                        <TableRow key={r.id}>
                          <TableCell>{r.parameterName}</TableCell>
                          <TableCell>{r.expectedValue ?? '—'}</TableCell>
                          <TableCell>{r.actualValue ?? '—'}</TableCell>
                          <TableCell>{r.unitOfMeasure ?? '—'}</TableCell>
                          <TableCell>
                            <InspectionOutcomeChip outcome={r.outcome} />
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </Box>
              )}
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Дефекты ({inspection.defects?.length ?? 0})
              </Typography>

              {!inspection.defects || inspection.defects.length === 0 ? (
                <Typography color="text.secondary">Дефекты не зарегистрированы.</Typography>
              ) : (
                <Stack spacing={1.5}>
                  {inspection.defects.map((d) => (
                    <Card
                      key={d.id}
                      variant="outlined"
                      sx={{ cursor: 'pointer', '&:hover': { borderColor: 'primary.main' } }}
                      onClick={() => navigate(`/defects/${d.id}`)}
                    >
                      <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
                        <Stack direction="row" justifyContent="space-between" spacing={1} sx={{ mb: 0.5 }}>
                          <Typography variant="subtitle2">{d.defectTypeName}</Typography>
                          <DefectSeverityChip severity={d.severity} />
                        </Stack>

                        <Typography variant="body2" color="text.secondary">
                          {d.description}
                        </Typography>

                        {d.location ? (
                          <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.5 }}>
                            Место: {d.location}
                          </Typography>
                        ) : null}
                      </CardContent>
                    </Card>
                  ))}
                </Stack>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </>
  );
}
