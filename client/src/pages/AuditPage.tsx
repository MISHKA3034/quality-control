import { useCallback } from 'react';
import { Card, CardContent, Chip, Grid, MenuItem, Tooltip, Typography, TextField } from '@mui/material';
import { auditApi } from '../api/endpoints';
import { DataTable, type Column } from '../components/DataTable';
import { PageHeader } from '../components/PageHeader';
import { usePagedData } from '../hooks/usePagedData';
import type { AuditLogEntry, AuditLogQuery } from '../types/api';
import { formatDateTime, toInputDate } from '../utils/format';
import { auditActionLabels } from '../utils/labels';

/** Renders the JSON diff the API stores for updates, without dumping raw JSON on screen. */
function ChangesCell({ changes }: { changes?: string }) {
  if (!changes) {
    return <>—</>;
  }

  try {
    const parsed = JSON.parse(changes) as Record<string, unknown>;
    const fields = Object.keys(parsed);
    const summary = fields.slice(0, 3).join(', ') + (fields.length > 3 ? `, +${fields.length - 3}` : '');

    return (
      <Tooltip
        title={
          <pre style={{ margin: 0, whiteSpace: 'pre-wrap', fontSize: 11 }}>
            {JSON.stringify(parsed, null, 2)}
          </pre>
        }
      >
        <Typography variant="body2" sx={{ cursor: 'help', textDecoration: 'underline dotted' }}>
          {summary}
        </Typography>
      </Tooltip>
    );
  } catch {
    return <>—</>;
  }
}

export function AuditPage() {
  const fetcher = useCallback((query: AuditLogQuery) => auditApi.getPaged(query), []);

  const { data, isLoading, query, setQuery } = usePagedData<AuditLogEntry, AuditLogQuery>(fetcher, {
    page: 1,
    pageSize: 50,
    sortBy: 'timestamp',
    sortDescending: true,
  });

  const columns: Column<AuditLogEntry>[] = [
    {
      id: 'timestamp',
      label: 'Время',
      sortable: true,
      width: 150,
      render: (l) => formatDateTime(l.timestamp),
    },
    { id: 'userName', label: 'Пользователь', sortable: true, width: 140, render: (l) => l.userName ?? '—' },
    {
      id: 'action',
      label: 'Действие',
      sortable: true,
      width: 170,
      render: (l) => (
        <Chip
          label={auditActionLabels[l.action] ?? l.action}
          size="small"
          color={l.isSuccess ? 'default' : 'error'}
          variant={l.isSuccess ? 'outlined' : 'filled'}
        />
      ),
    },
    { id: 'entityName', label: 'Объект', sortable: true, width: 140, render: (l) => l.entityName ?? '—' },
    { id: 'entityId', label: 'ID', width: 70, render: (l) => l.entityId ?? '—' },
    { id: 'description', label: 'Описание', render: (l) => l.description ?? '—' },
    { id: 'changes', label: 'Изменения', width: 180, render: (l) => <ChangesCell changes={l.changes} /> },
    { id: 'ipAddress', label: 'IP', width: 120, render: (l) => l.ipAddress ?? '—' },
  ];

  return (
    <>
      <PageHeader
        title="Журнал действий"
        subtitle="История всех операций пользователей. Записи журнала не редактируются и не удаляются."
      />

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} md={3}>
              <TextField
                label="Поиск (описание, пользователь)"
                fullWidth
                value={query.search ?? ''}
                onChange={(e) => setQuery({ search: e.target.value || undefined })}
              />
            </Grid>

            <Grid item xs={12} sm={6} md={3}>
              <TextField
                select
                label="Действие"
                fullWidth
                value={query.action ?? ''}
                onChange={(e) => setQuery({ action: (e.target.value || undefined) as never })}
              >
                <MenuItem value="">Все действия</MenuItem>
                {Object.entries(auditActionLabels).map(([value, label]) => (
                  <MenuItem key={value} value={value}>
                    {label}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

            <Grid item xs={12} sm={6} md={2}>
              <TextField
                select
                label="Результат"
                fullWidth
                value={query.isSuccess === undefined ? '' : String(query.isSuccess)}
                onChange={(e) =>
                  setQuery({ isSuccess: e.target.value === '' ? undefined : e.target.value === 'true' })
                }
              >
                <MenuItem value="">Все</MenuItem>
                <MenuItem value="true">Успешные</MenuItem>
                <MenuItem value="false">Отклонённые</MenuItem>
              </TextField>
            </Grid>

            <Grid item xs={6} md={2}>
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

            <Grid item xs={6} md={2}>
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
        getRowKey={(l) => l.id}
        emptyMessage="Записи журнала не найдены."
      />
    </>
  );
}
