import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Card, CardContent, Chip, Grid, MenuItem, TextField } from '@mui/material';
import { PictureAsPdf as PdfIcon, TableChart as ExcelIcon } from '@mui/icons-material';
import { defectTypesApi, defectsApi, departmentsApi, reportsApi } from '../api/endpoints';
import { DataTable, type Column } from '../components/DataTable';
import { PageHeader } from '../components/PageHeader';
import { DefectSeverityChip } from '../components/StatusChip';
import { useNotification } from '../contexts/NotificationContext';
import { usePagedData } from '../hooks/usePagedData';
import type { Defect, DefectQuery, DefectType, Department } from '../types/api';
import { formatDate, toInputDate } from '../utils/format';
import { defectSeverityLabels } from '../utils/labels';

export function DefectsPage() {
  const navigate = useNavigate();
  const { notifyError } = useNotification();
  const [departments, setDepartments] = useState<Department[]>([]);
  const [defectTypes, setDefectTypes] = useState<DefectType[]>([]);
  const [isExporting, setIsExporting] = useState(false);

  const fetcher = useCallback((query: DefectQuery) => defectsApi.getPaged(query), []);

  const { data, isLoading, query, setQuery } = usePagedData<Defect, DefectQuery>(fetcher, {
    page: 1,
    pageSize: 20,
    sortBy: 'detectedAt',
    sortDescending: true,
  });

  useEffect(() => {
    departmentsApi.lookup().then(setDepartments).catch(notifyError);
    defectTypesApi.lookup().then(setDefectTypes).catch(notifyError);
  }, [notifyError]);

  async function handleExport(format: 'Excel' | 'Pdf') {
    setIsExporting(true);

    try {
      await reportsApi.exportDefects({ ...query, format });
    } catch (error) {
      notifyError(error);
    } finally {
      setIsExporting(false);
    }
  }

  const columns: Column<Defect>[] = [
    { id: 'detectedAt', label: 'Дата', sortable: true, width: 110, render: (d) => formatDate(d.detectedAt) },
    { id: 'inspectionNumber', label: '№ акта', width: 140, render: (d) => d.inspectionNumber },
    { id: 'batchNumber', label: 'Партия', sortable: true, width: 150, render: (d) => d.batchNumber },
    { id: 'product', label: 'Изделие', sortable: true, render: (d) => d.productName },
    { id: 'defectType', label: 'Вид дефекта', sortable: true, render: (d) => d.defectTypeName },
    { id: 'description', label: 'Описание', render: (d) => d.description },
    {
      id: 'severity',
      label: 'Критичность',
      sortable: true,
      width: 150,
      render: (d) => <DefectSeverityChip severity={d.severity} />,
    },
    {
      id: 'isOpen',
      label: 'Состояние',
      width: 110,
      render: (d) =>
        d.isOpen ? (
          <Chip label="Открыт" size="small" color="warning" />
        ) : (
          <Chip label="Закрыт" size="small" color="success" />
        ),
    },
    { id: 'photoCount', label: 'Фото', align: 'right', width: 70, render: (d) => d.photoCount },
  ];

  return (
    <>
      <PageHeader
        title="Дефекты"
        subtitle="Зарегистрированные несоответствия"
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
                label="Поиск (описание, место, партия)"
                fullWidth
                value={query.search ?? ''}
                onChange={(e) => setQuery({ search: e.target.value || undefined })}
              />
            </Grid>

            <Grid item xs={12} sm={6} md={2}>
              <TextField
                select
                label="Вид дефекта"
                fullWidth
                value={query.defectTypeId ?? ''}
                onChange={(e) => setQuery({ defectTypeId: e.target.value ? Number(e.target.value) : undefined })}
              >
                <MenuItem value="">Все</MenuItem>
                {defectTypes.map((t) => (
                  <MenuItem key={t.id} value={t.id}>
                    {t.name}
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

            <Grid item xs={6} sm={4} md={2}>
              <TextField
                select
                label="Критичность"
                fullWidth
                value={query.severity ?? ''}
                onChange={(e) => setQuery({ severity: (e.target.value || undefined) as never })}
              >
                <MenuItem value="">Все</MenuItem>
                {Object.entries(defectSeverityLabels).map(([value, label]) => (
                  <MenuItem key={value} value={value}>
                    {label}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

            <Grid item xs={6} sm={4} md={1.5}>
              <TextField
                select
                label="Состояние"
                fullWidth
                value={query.isOpen === undefined ? '' : String(query.isOpen)}
                onChange={(e) =>
                  setQuery({ isOpen: e.target.value === '' ? undefined : e.target.value === 'true' })
                }
              >
                <MenuItem value="">Все</MenuItem>
                <MenuItem value="true">Открытые</MenuItem>
                <MenuItem value="false">Закрытые</MenuItem>
              </TextField>
            </Grid>

            <Grid item xs={6} sm={2} md={0.75}>
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

            <Grid item xs={6} sm={2} md={0.75}>
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
        onRowClick={(d) => navigate(`/defects/${d.id}`)}
        getRowKey={(d) => d.id}
        emptyMessage="Дефекты не найдены."
      />
    </>
  );
}
