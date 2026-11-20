import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Card, CardContent, Grid, MenuItem, TextField } from '@mui/material';
import {
  Add as AddIcon,
  PictureAsPdf as PdfIcon,
  TableChart as ExcelIcon,
} from '@mui/icons-material';
import { departmentsApi, employeesApi, inspectionsApi, reportsApi } from '../api/endpoints';
import { DataTable, type Column } from '../components/DataTable';
import { PageHeader } from '../components/PageHeader';
import { InspectionOutcomeChip } from '../components/StatusChip';
import { useAuth } from '../contexts/AuthContext';
import { useNotification } from '../contexts/NotificationContext';
import { usePagedData } from '../hooks/usePagedData';
import type { Department, Employee, QualityInspection, QualityInspectionQuery } from '../types/api';
import { formatDate, formatNumber, toInputDate } from '../utils/format';
import { inspectionOutcomeLabels } from '../utils/labels';

export function InspectionsPage() {
  const navigate = useNavigate();
  const { hasRole } = useAuth();
  const { notifyError } = useNotification();
  const [departments, setDepartments] = useState<Department[]>([]);
  const [inspectors, setInspectors] = useState<Employee[]>([]);
  const [isExporting, setIsExporting] = useState(false);

  const canInspect = hasRole('Administrator', 'QualityInspector');

  const fetcher = useCallback((query: QualityInspectionQuery) => inspectionsApi.getPaged(query), []);

  const { data, isLoading, query, setQuery } = usePagedData<QualityInspection, QualityInspectionQuery>(
    fetcher,
    { page: 1, pageSize: 20, sortBy: 'inspectionDate', sortDescending: true },
  );

  useEffect(() => {
    departmentsApi.lookup().then(setDepartments).catch(notifyError);
    employeesApi.lookup().then(setInspectors).catch(notifyError);
  }, [notifyError]);

  async function handleExport(format: 'Excel' | 'Pdf') {
    setIsExporting(true);

    try {
      // The current filter is passed through, so the file matches the screen.
      await reportsApi.exportInspections({ ...query, format });
    } catch (error) {
      notifyError(error);
    } finally {
      setIsExporting(false);
    }
  }

  const columns: Column<QualityInspection>[] = [
    { id: 'inspectionNumber', label: '№ акта', sortable: true, width: 150, render: (i) => i.inspectionNumber },
    {
      id: 'inspectionDate',
      label: 'Дата',
      sortable: true,
      width: 110,
      render: (i) => formatDate(i.inspectionDate),
    },
    { id: 'batchNumber', label: 'Партия', sortable: true, width: 150, render: (i) => i.batchNumber },
    { id: 'product', label: 'Изделие', sortable: true, render: (i) => i.productName },
    { id: 'department', label: 'Участок', render: (i) => i.departmentName },
    { id: 'inspector', label: 'Контролёр', sortable: true, render: (i) => i.inspectorName },
    {
      id: 'outcome',
      label: 'Результат',
      sortable: true,
      width: 200,
      render: (i) => <InspectionOutcomeChip outcome={i.outcome} />,
    },
    {
      id: 'quantities',
      label: 'Пров./брак',
      align: 'right',
      width: 110,
      render: (i) => `${formatNumber(i.quantityInspected)} / ${formatNumber(i.quantityRejected)}`,
    },
    { id: 'defectCount', label: 'Дефектов', align: 'right', width: 90, render: (i) => i.defectCount },
  ];

  return (
    <>
      <PageHeader
        title="Проверки качества"
        subtitle="Акты контроля ОТК"
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

            {canInspect ? (
              <Button variant="contained" startIcon={<AddIcon />} onClick={() => navigate('/inspections/new')}>
                Оформить акт
              </Button>
            ) : null}
          </>
        }
      />

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} md={3}>
              <TextField
                label="Поиск (акт, партия, изделие)"
                fullWidth
                value={query.search ?? ''}
                onChange={(e) => setQuery({ search: e.target.value || undefined })}
              />
            </Grid>

            <Grid item xs={12} sm={6} md={2}>
              <TextField
                select
                label="Результат"
                fullWidth
                value={query.outcome ?? ''}
                onChange={(e) => setQuery({ outcome: (e.target.value || undefined) as never })}
              >
                <MenuItem value="">Все</MenuItem>
                {Object.entries(inspectionOutcomeLabels).map(([value, label]) => (
                  <MenuItem key={value} value={value}>
                    {label}
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

            <Grid item xs={12} sm={6} md={2}>
              <TextField
                select
                label="Контролёр"
                fullWidth
                value={query.inspectorId ?? ''}
                onChange={(e) => setQuery({ inspectorId: e.target.value ? Number(e.target.value) : undefined })}
              >
                <MenuItem value="">Все</MenuItem>
                {inspectors.map((e) => (
                  <MenuItem key={e.id} value={e.id}>
                    {e.shortName}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

            <Grid item xs={6} md={1.5}>
              <TextField
                type="date"
                label="С"
                fullWidth
                InputLabelProps={{ shrink: true }}
                value={query.dateFrom ? toInputDate(query.dateFrom) : ''}
                onChange={(e) =>
                  setQuery({ dateFrom: e.target.value ? new Date(e.target.value).toISOString() : undefined })
                }
              />
            </Grid>

            <Grid item xs={6} md={1.5}>
              <TextField
                type="date"
                label="По"
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
        onRowClick={(i) => navigate(`/inspections/${i.id}`)}
        getRowKey={(i) => i.id}
        emptyMessage="Акты проверки не найдены."
      />
    </>
  );
}
