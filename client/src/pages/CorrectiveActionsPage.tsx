import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Card, CardContent, Chip, Grid, MenuItem, TextField } from '@mui/material';
import { PictureAsPdf as PdfIcon, TableChart as ExcelIcon } from '@mui/icons-material';
import { correctiveActionsApi, departmentsApi, employeesApi, reportsApi } from '../api/endpoints';
import { DataTable, type Column } from '../components/DataTable';
import { PageHeader } from '../components/PageHeader';
import { CorrectiveActionStatusChip, DefectSeverityChip } from '../components/StatusChip';
import { useNotification } from '../contexts/NotificationContext';
import { usePagedData } from '../hooks/usePagedData';
import type { CorrectiveAction, CorrectiveActionQuery, Department, Employee } from '../types/api';
import { formatDate } from '../utils/format';
import { correctiveActionStatusLabels } from '../utils/labels';

export function CorrectiveActionsPage() {
  const navigate = useNavigate();
  const { notifyError } = useNotification();
  const [departments, setDepartments] = useState<Department[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [isExporting, setIsExporting] = useState(false);

  const fetcher = useCallback((query: CorrectiveActionQuery) => correctiveActionsApi.getPaged(query), []);

  const { data, isLoading, query, setQuery } = usePagedData<CorrectiveAction, CorrectiveActionQuery>(fetcher, {
    page: 1,
    pageSize: 20,
    sortBy: 'createdAt',
    sortDescending: true,
  });

  useEffect(() => {
    departmentsApi.lookup().then(setDepartments).catch(notifyError);
    employeesApi.lookup().then(setEmployees).catch(notifyError);
  }, [notifyError]);

  async function handleExport(format: 'Excel' | 'Pdf') {
    setIsExporting(true);

    try {
      await reportsApi.exportCorrectiveActions({ ...query, format });
    } catch (error) {
      notifyError(error);
    } finally {
      setIsExporting(false);
    }
  }

  const columns: Column<CorrectiveAction>[] = [
    { id: 'id', label: '№', width: 70, render: (a) => a.id },
    { id: 'createdAt', label: 'Создано', sortable: true, width: 110, render: (a) => formatDate(a.createdAt) },
    { id: 'batchNumber', label: 'Партия', sortable: true, width: 150, render: (a) => a.batchNumber },
    { id: 'defect', label: 'Дефект', render: (a) => a.defectDescription },
    {
      id: 'severity',
      label: 'Критичность',
      sortable: true,
      width: 150,
      render: (a) => <DefectSeverityChip severity={a.defectSeverity} />,
    },
    { id: 'assignedTo', label: 'Ответственный', sortable: true, render: (a) => a.assignedToName ?? '—' },
    {
      id: 'dueDate',
      label: 'Срок',
      sortable: true,
      width: 130,
      render: (a) =>
        a.dueDate ? (
          <Chip
            label={formatDate(a.dueDate)}
            size="small"
            color={a.isOverdue ? 'error' : 'default'}
            variant={a.isOverdue ? 'filled' : 'outlined'}
          />
        ) : (
          '—'
        ),
    },
    {
      id: 'status',
      label: 'Статус',
      sortable: true,
      width: 130,
      render: (a) => <CorrectiveActionStatusChip status={a.status} />,
    },
  ];

  return (
    <>
      <PageHeader
        title="Корректирующие действия"
        subtitle="Устранение дефектов и контроль исполнения"
        actions={
          <>
            <Button
              variant="outlined"
              startIcon={<ExcelIcon />}
              disabled={isExporting}
              onClick={() => void handleExport('Excel')}
            >
              Excel
            </Button>
            <Button
              variant="outlined"
              startIcon={<PdfIcon />}
              disabled={isExporting}
              onClick={() => void handleExport('Pdf')}
            >
              PDF
            </Button>
          </>
        }
      />

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} md={3}>
              <TextField
                label="Поиск (мероприятие, дефект, партия)"
                fullWidth
                value={query.search ?? ''}
                onChange={(e) => setQuery({ search: e.target.value || undefined })}
              />
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
                {Object.entries(correctiveActionStatusLabels).map(([value, label]) => (
                  <MenuItem key={value} value={value}>
                    {label}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

            <Grid item xs={12} sm={6} md={2}>
              <TextField
                select
                label="Ответственный"
                fullWidth
                value={query.assignedToId ?? ''}
                onChange={(e) => setQuery({ assignedToId: e.target.value ? Number(e.target.value) : undefined })}
              >
                <MenuItem value="">Все</MenuItem>
                {employees.map((e) => (
                  <MenuItem key={e.id} value={e.id}>
                    {e.shortName}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

            <Grid item xs={12} sm={6} md={2}>
              <TextField
                select
                label="Подразделение"
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

            <Grid item xs={12} sm={6} md={3}>
              <TextField
                select
                label="Срок исполнения"
                fullWidth
                value={query.isOverdue === undefined ? '' : String(query.isOverdue)}
                onChange={(e) =>
                  setQuery({ isOverdue: e.target.value === '' ? undefined : e.target.value === 'true' })
                }
              >
                <MenuItem value="">Все</MenuItem>
                <MenuItem value="true">Только просроченные</MenuItem>
                <MenuItem value="false">Без просрочки</MenuItem>
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
        onRowClick={(a) => navigate(`/corrective-actions/${a.id}`)}
        getRowKey={(a) => a.id}
        emptyMessage="Корректирующие действия не найдены."
      />
    </>
  );
}
