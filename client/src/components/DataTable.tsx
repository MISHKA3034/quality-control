import type { ReactNode } from 'react';
import {
  Box,
  CircularProgress,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TableSortLabel,
  Typography,
} from '@mui/material';
import type { PagedResult } from '../types/api';

export interface Column<T> {
  /** Sort key sent to the API. Must match the endpoint's allowlist, or sorting is ignored. */
  id: string;
  label: string;
  sortable?: boolean;
  align?: 'left' | 'right' | 'center';
  width?: number | string;
  render: (row: T) => ReactNode;
}

interface DataTableProps<T> {
  columns: Column<T>[];
  data: PagedResult<T> | null;
  isLoading: boolean;
  sortBy?: string;
  sortDescending?: boolean;
  onSortChange: (sortBy: string, sortDescending: boolean) => void;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
  onRowClick?: (row: T) => void;
  getRowKey: (row: T) => string | number;
  emptyMessage?: string;
}

/**
 * Server-paged, server-sorted table.
 *
 * Deliberately not MUI DataGrid: the free tier lacks server-side sorting and
 * paging as first-class features, and this API pages everything. A plain Table
 * keeps the server the single source of truth for what is on screen.
 */
export function DataTable<T>({
  columns,
  data,
  isLoading,
  sortBy,
  sortDescending = false,
  onSortChange,
  onPageChange,
  onPageSizeChange,
  onRowClick,
  getRowKey,
  emptyMessage = 'Нет данных.',
}: DataTableProps<T>) {
  function handleSort(columnId: string) {
    // Clicking the active column flips direction; a new column starts ascending.
    const isActive = sortBy === columnId;
    onSortChange(columnId, isActive ? !sortDescending : false);
  }

  const rows = data?.items ?? [];
  const showEmpty = !isLoading && rows.length === 0;

  return (
    <Paper variant="outlined">
      <TableContainer sx={{ position: 'relative' }}>
        {/* Overlays instead of replacing the table, so the layout does not jump
            and the previous page stays readable while the next one loads. */}
        {isLoading ? (
          <Box
            sx={{
              position: 'absolute',
              inset: 0,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              bgcolor: 'rgba(255,255,255,0.6)',
              zIndex: 2,
            }}
          >
            <CircularProgress size={32} />
          </Box>
        ) : null}

        <Table size="small" stickyHeader>
          <TableHead>
            <TableRow>
              {columns.map((column) => (
                <TableCell
                  key={column.id}
                  align={column.align ?? 'left'}
                  sx={{ width: column.width }}
                  sortDirection={sortBy === column.id ? (sortDescending ? 'desc' : 'asc') : false}
                >
                  {column.sortable ? (
                    <TableSortLabel
                      active={sortBy === column.id}
                      direction={sortBy === column.id && sortDescending ? 'desc' : 'asc'}
                      onClick={() => handleSort(column.id)}
                    >
                      {column.label}
                    </TableSortLabel>
                  ) : (
                    column.label
                  )}
                </TableCell>
              ))}
            </TableRow>
          </TableHead>

          <TableBody>
            {rows.map((row) => (
              <TableRow
                key={getRowKey(row)}
                hover={onRowClick !== undefined}
                onClick={onRowClick ? () => onRowClick(row) : undefined}
                sx={{ cursor: onRowClick ? 'pointer' : 'default' }}
              >
                {columns.map((column) => (
                  <TableCell key={column.id} align={column.align ?? 'left'}>
                    {column.render(row)}
                  </TableCell>
                ))}
              </TableRow>
            ))}

            {showEmpty ? (
              <TableRow>
                <TableCell colSpan={columns.length} sx={{ py: 6, textAlign: 'center' }}>
                  <Typography color="text.secondary">{emptyMessage}</Typography>
                </TableCell>
              </TableRow>
            ) : null}
          </TableBody>
        </Table>
      </TableContainer>

      <TablePagination
        component="div"
        count={data?.totalCount ?? 0}
        // MUI counts pages from zero; the API counts from one.
        page={data ? Math.max(data.page - 1, 0) : 0}
        onPageChange={(_, page) => onPageChange(page + 1)}
        rowsPerPage={data?.pageSize ?? 20}
        onRowsPerPageChange={(event) => onPageSizeChange(Number(event.target.value))}
        rowsPerPageOptions={[10, 20, 50, 100]}
        labelRowsPerPage="Строк на странице:"
        labelDisplayedRows={({ from, to, count }) => `${from}–${to} из ${count}`}
      />
    </Paper>
  );
}
