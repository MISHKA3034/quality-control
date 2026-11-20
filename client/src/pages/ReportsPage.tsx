import { useEffect, useState } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  Grid,
  MenuItem,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import { PictureAsPdf as PdfIcon, TableChart as ExcelIcon } from '@mui/icons-material';
import { departmentsApi, reportsApi } from '../api/endpoints';
import { PageHeader } from '../components/PageHeader';
import { useNotification } from '../contexts/NotificationContext';
import type {
  Department,
  DefectTypeStatistics,
  DepartmentStatistics,
  ReportPeriodQuery,
} from '../types/api';
import { formatNumber, formatPercent, toInputDate } from '../utils/format';

export function ReportsPage() {
  const { notifyError } = useNotification();
  const [departments, setDepartments] = useState<Department[]>([]);
  const [byDepartment, setByDepartment] = useState<DepartmentStatistics[]>([]);
  const [topDefects, setTopDefects] = useState<DefectTypeStatistics[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isExporting, setIsExporting] = useState(false);

  const [period, setPeriod] = useState<ReportPeriodQuery>({});

  useEffect(() => {
    departmentsApi.lookup().then(setDepartments).catch(notifyError);
  }, [notifyError]);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);

    Promise.all([
      reportsApi.getDepartmentStatistics(period),
      reportsApi.getTopDefectTypes({ ...period, top: 20 }),
    ])
      .then(([departmentStats, defectStats]) => {
        if (!cancelled) {
          setByDepartment(departmentStats);
          setTopDefects(defectStats);
        }
      })
      .catch((error: unknown) => {
        if (!cancelled) {
          notifyError(error);
        }
      })
      .finally(() => {
        if (!cancelled) {
          setIsLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [period, notifyError]);

  async function exportReport(
    kind: 'departments' | 'topDefects',
    format: 'Excel' | 'Pdf',
  ) {
    setIsExporting(true);

    try {
      if (kind === 'departments') {
        await reportsApi.exportDepartments({ ...period, format });
      } else {
        await reportsApi.exportTopDefectTypes({ ...period, format });
      }
    } catch (error) {
      notifyError(error);
    } finally {
      setIsExporting(false);
    }
  }

  return (
    <>
      <PageHeader title="Отчёты" subtitle="Аналитика качества и выгрузка данных" />

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Typography variant="subtitle2" gutterBottom>
            Период и фильтры
          </Typography>

          <Grid container spacing={2}>
            <Grid item xs={12} sm={4} md={3}>
              <TextField
                type="date"
                label="Дата с"
                fullWidth
                InputLabelProps={{ shrink: true }}
                value={period.dateFrom ? toInputDate(period.dateFrom) : ''}
                onChange={(e) =>
                  setPeriod((p) => ({
                    ...p,
                    dateFrom: e.target.value ? new Date(e.target.value).toISOString() : undefined,
                  }))
                }
                helperText="По умолчанию — последние 12 месяцев"
              />
            </Grid>

            <Grid item xs={12} sm={4} md={3}>
              <TextField
                type="date"
                label="Дата по"
                fullWidth
                InputLabelProps={{ shrink: true }}
                value={period.dateTo ? toInputDate(period.dateTo) : ''}
                onChange={(e) =>
                  setPeriod((p) => ({
                    ...p,
                    dateTo: e.target.value ? new Date(e.target.value).toISOString() : undefined,
                  }))
                }
              />
            </Grid>

            <Grid item xs={12} sm={4} md={3}>
              <TextField
                select
                label="Подразделение"
                fullWidth
                value={period.departmentId ?? ''}
                onChange={(e) =>
                  setPeriod((p) => ({
                    ...p,
                    departmentId: e.target.value ? Number(e.target.value) : undefined,
                  }))
                }
              >
                <MenuItem value="">Все подразделения</MenuItem>
                {departments.map((d) => (
                  <MenuItem key={d.id} value={d.id}>
                    {d.name}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            justifyContent="space-between"
            alignItems={{ sm: 'center' }}
            spacing={1}
            sx={{ mb: 2 }}
          >
            <Typography variant="h6">Статистика по подразделениям</Typography>

            <Stack direction="row" spacing={1}>
              <Button
                size="small"
                variant="outlined"
                startIcon={<ExcelIcon />}
                disabled={isExporting}
                onClick={() => void exportReport('departments', 'Excel')}
              >
                Excel
              </Button>
              <Button
                size="small"
                variant="outlined"
                startIcon={<PdfIcon />}
                disabled={isExporting}
                onClick={() => void exportReport('departments', 'Pdf')}
              >
                PDF
              </Button>
            </Stack>
          </Stack>

          <Box sx={{ overflowX: 'auto' }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Подразделение</TableCell>
                  <TableCell align="right">Проверок</TableCell>
                  <TableCell align="right">Проверено изделий</TableCell>
                  <TableCell align="right">Забраковано</TableCell>
                  <TableCell align="right">Процент брака</TableCell>
                  <TableCell align="right">Дефектов</TableCell>
                  <TableCell align="right">Открытых</TableCell>
                </TableRow>
              </TableHead>

              <TableBody>
                {byDepartment.map((d) => (
                  <TableRow key={d.departmentId}>
                    <TableCell>{d.departmentName}</TableCell>
                    <TableCell align="right">{formatNumber(d.inspectionCount)}</TableCell>
                    <TableCell align="right">{formatNumber(d.unitsInspected)}</TableCell>
                    <TableCell align="right">{formatNumber(d.unitsRejected)}</TableCell>
                    <TableCell align="right">{formatPercent(d.defectRatePercent)}</TableCell>
                    <TableCell align="right">{formatNumber(d.defectCount)}</TableCell>
                    <TableCell align="right">{formatNumber(d.openDefectCount)}</TableCell>
                  </TableRow>
                ))}

                {!isLoading && byDepartment.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} sx={{ textAlign: 'center', py: 4 }}>
                      <Typography color="text.secondary">Нет данных за выбранный период.</Typography>
                    </TableCell>
                  </TableRow>
                ) : null}
              </TableBody>
            </Table>
          </Box>
        </CardContent>
      </Card>

      <Card>
        <CardContent>
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            justifyContent="space-between"
            alignItems={{ sm: 'center' }}
            spacing={1}
            sx={{ mb: 2 }}
          >
            <Typography variant="h6">Наиболее частые дефекты</Typography>

            <Stack direction="row" spacing={1}>
              <Button
                size="small"
                variant="outlined"
                startIcon={<ExcelIcon />}
                disabled={isExporting}
                onClick={() => void exportReport('topDefects', 'Excel')}
              >
                Excel
              </Button>
              <Button
                size="small"
                variant="outlined"
                startIcon={<PdfIcon />}
                disabled={isExporting}
                onClick={() => void exportReport('topDefects', 'Pdf')}
              >
                PDF
              </Button>
            </Stack>
          </Stack>

          <Box sx={{ overflowX: 'auto' }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Код</TableCell>
                  <TableCell>Вид дефекта</TableCell>
                  <TableCell align="right">Количество</TableCell>
                  <TableCell align="right">Изделий затронуто</TableCell>
                  <TableCell align="right">Доля</TableCell>
                  <TableCell align="right">Критических</TableCell>
                </TableRow>
              </TableHead>

              <TableBody>
                {topDefects.map((d) => (
                  <TableRow key={d.defectTypeId}>
                    <TableCell>{d.defectTypeCode}</TableCell>
                    <TableCell>{d.defectTypeName}</TableCell>
                    <TableCell align="right">{formatNumber(d.defectCount)}</TableCell>
                    <TableCell align="right">{formatNumber(d.affectedUnits)}</TableCell>
                    <TableCell align="right">{formatPercent(d.sharePercent)}</TableCell>
                    <TableCell align="right">{formatNumber(d.criticalCount)}</TableCell>
                  </TableRow>
                ))}

                {!isLoading && topDefects.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} sx={{ textAlign: 'center', py: 4 }}>
                      <Typography color="text.secondary">Дефекты за период не зарегистрированы.</Typography>
                    </TableCell>
                  </TableRow>
                ) : null}
              </TableBody>
            </Table>
          </Box>
        </CardContent>
      </Card>
    </>
  );
}
