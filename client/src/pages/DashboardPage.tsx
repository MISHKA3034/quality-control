import { useEffect, useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Grid,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import {
  Assignment as AssignmentIcon,
  BugReport as BugReportIcon,
  CheckCircle as CheckCircleIcon,
  ErrorOutline as ErrorOutlineIcon,
  Schedule as ScheduleIcon,
  TrendingDown as TrendingDownIcon,
} from '@mui/icons-material';
import { BarChart } from '@mui/x-charts/BarChart';
import { LineChart } from '@mui/x-charts/LineChart';
import { PieChart } from '@mui/x-charts/PieChart';
import type { SvgIconComponent } from '@mui/icons-material';
import { reportsApi } from '../api/endpoints';
import { PageHeader } from '../components/PageHeader';
import { useNotification } from '../contexts/NotificationContext';
import type { Dashboard } from '../types/api';
import { formatDate, formatNumber, formatPercent } from '../utils/format';

interface StatCardProps {
  title: string;
  value: string;
  hint?: string;
  icon: SvgIconComponent;
  colour: string;
}

function StatCard({ title, value, hint, icon: Icon, colour }: StatCardProps) {
  return (
    <Card sx={{ height: '100%' }}>
      <CardContent>
        <Stack direction="row" spacing={2} alignItems="flex-start">
          <Box
            sx={{
              width: 44,
              height: 44,
              borderRadius: 1.5,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              bgcolor: `${colour}14`,
              color: colour,
              flexShrink: 0,
            }}
          >
            <Icon />
          </Box>

          <Box sx={{ minWidth: 0 }}>
            <Typography
              variant="body2"
              color="text.secondary"
              // No noWrap: at six cards per row the titles ("Процент брака") are
              // wider than the card and would truncate. Let them wrap to two lines
              // and reserve the height so the value rows stay aligned across cards.
              sx={{ lineHeight: 1.25, minHeight: 34 }}
            >
              {title}
            </Typography>
            <Typography variant="h5" sx={{ mt: 0.5 }}>
              {value}
            </Typography>
            {hint ? (
              <Typography variant="caption" color="text.secondary">
                {hint}
              </Typography>
            ) : null}
          </Box>
        </Stack>
      </CardContent>
    </Card>
  );
}

export function DashboardPage() {
  const [dashboard, setDashboard] = useState<Dashboard | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const { notifyError } = useNotification();

  useEffect(() => {
    let cancelled = false;

    // No period given: the API defaults to the last 12 months.
    reportsApi
      .getDashboard({})
      .then((data) => {
        if (!cancelled) {
          setDashboard(data);
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
  }, [notifyError]);

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!dashboard) {
    return <Typography color="text.secondary">Не удалось загрузить данные дашборда.</Typography>;
  }

  const { summary, byDepartment, topDefectTypes, monthlyTrend } = dashboard;

  return (
    <>
      <PageHeader
        title="Дашборд"
        subtitle={`Период: ${formatDate(dashboard.periodFrom)} — ${formatDate(dashboard.periodTo)}`}
      />

      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={6} md={4} lg={2}>
          <StatCard
            title="Проверок"
            value={formatNumber(summary.totalInspections)}
            hint={`партий: ${formatNumber(summary.totalBatchesInspected)}`}
            icon={AssignmentIcon}
            colour="#1B4F72"
          />
        </Grid>

        <Grid item xs={12} sm={6} md={4} lg={2}>
          <StatCard
            title="Процент брака"
            value={formatPercent(summary.defectRatePercent)}
            hint={`${formatNumber(summary.totalUnitsRejected)} из ${formatNumber(summary.totalUnitsInspected)} шт.`}
            icon={TrendingDownIcon}
            colour="#B03A2E"
          />
        </Grid>

        <Grid item xs={12} sm={6} md={4} lg={2}>
          <StatCard
            title="Открытых дефектов"
            value={formatNumber(summary.openDefects)}
            hint={`критических: ${formatNumber(summary.criticalOpenDefects)}`}
            icon={BugReportIcon}
            colour="#B9770E"
          />
        </Grid>

        <Grid item xs={12} sm={6} md={4} lg={2}>
          <StatCard
            title="Закрыто замечаний"
            value={formatNumber(summary.closedCorrectiveActions)}
            icon={CheckCircleIcon}
            colour="#1E8449"
          />
        </Grid>

        <Grid item xs={12} sm={6} md={4} lg={2}>
          <StatCard
            title="Просрочено КД"
            value={formatNumber(summary.overdueCorrectiveActions)}
            icon={ScheduleIcon}
            colour="#7D3C98"
          />
        </Grid>

        <Grid item xs={12} sm={6} md={4} lg={2}>
          <StatCard
            title="Критических (открыто)"
            value={formatNumber(summary.criticalOpenDefects)}
            hint="требуют решения"
            icon={ErrorOutlineIcon}
            colour="#C0392B"
          />
        </Grid>
      </Grid>

      <Grid container spacing={2}>
        <Grid item xs={12} lg={8}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Динамика брака по месяцам
              </Typography>

              {monthlyTrend.length === 0 ? (
                <Typography color="text.secondary">Нет данных за период.</Typography>
              ) : (
                <LineChart
                  height={300}
                  xAxis={[{ scaleType: 'point', data: monthlyTrend.map((p) => p.label) }]}
                  yAxis={[{ label: '% брака' }]}
                  series={[
                    {
                      data: monthlyTrend.map((p) => p.defectRatePercent),
                      label: 'Процент брака',
                      color: '#B03A2E',
                      // Months with no inspections are real zeros, not gaps.
                      valueFormatter: (v) => (v === null ? '—' : `${v.toFixed(2)} %`),
                      area: true,
                      showMark: true,
                    },
                  ]}
                  margin={{ left: 60, right: 20, top: 20, bottom: 40 }}
                />
              )}
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} lg={4}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Результаты проверок
              </Typography>

              {summary.totalInspections === 0 ? (
                <Typography color="text.secondary">Нет данных за период.</Typography>
              ) : (
                <PieChart
                  height={300}
                  series={[
                    {
                      data: [
                        {
                          id: 0,
                          value: summary.inspectionsConforming,
                          label: 'Соответствует',
                          color: '#1E8449',
                        },
                        {
                          id: 1,
                          value: summary.inspectionsReworkRequired,
                          label: 'Доработка',
                          color: '#B9770E',
                        },
                        { id: 2, value: summary.inspectionsDefective, label: 'Брак', color: '#B03A2E' },
                      ],
                      innerRadius: 50,
                      paddingAngle: 2,
                      cornerRadius: 3,
                    },
                  ]}
                  margin={{ top: 20, bottom: 60 }}
                  slotProps={{
                    legend: { direction: 'row', position: { vertical: 'bottom', horizontal: 'middle' } },
                  }}
                />
              )}
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} lg={6}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Самые частые дефекты
              </Typography>

              {topDefectTypes.length === 0 ? (
                <Typography color="text.secondary">Дефекты за период не зарегистрированы.</Typography>
              ) : (
                <BarChart
                  height={320}
                  layout="horizontal"
                  yAxis={[
                    {
                      scaleType: 'band',
                      data: topDefectTypes.slice(0, 7).map((d) => d.defectTypeName),
                    },
                  ]}
                  series={[
                    {
                      data: topDefectTypes.slice(0, 7).map((d) => d.defectCount),
                      label: 'Количество',
                      color: '#2874A6',
                    },
                  ]}
                  // Room for the defect names, which are long in Russian.
                  margin={{ left: 220, right: 20, top: 20, bottom: 40 }}
                />
              )}
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} lg={6}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Статистика по подразделениям
              </Typography>

              {byDepartment.length === 0 ? (
                <Typography color="text.secondary">Нет данных за период.</Typography>
              ) : (
                <Box sx={{ overflowX: 'auto' }}>
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell>Подразделение</TableCell>
                        <TableCell align="right">Проверок</TableCell>
                        <TableCell align="right">Брак</TableCell>
                        <TableCell align="right">Открыто</TableCell>
                      </TableRow>
                    </TableHead>

                    <TableBody>
                      {byDepartment.map((d) => (
                        <TableRow key={d.departmentId}>
                          <TableCell>{d.departmentName}</TableCell>
                          <TableCell align="right">{formatNumber(d.inspectionCount)}</TableCell>
                          <TableCell align="right">
                            <Chip
                              label={formatPercent(d.defectRatePercent)}
                              size="small"
                              // Thresholds are a display aid, not a plant standard.
                              color={
                                d.defectRatePercent >= 8
                                  ? 'error'
                                  : d.defectRatePercent >= 4
                                    ? 'warning'
                                    : 'success'
                              }
                            />
                          </TableCell>
                          <TableCell align="right">{formatNumber(d.openDefectCount)}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </Box>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </>
  );
}
