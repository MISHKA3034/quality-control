import { format, parseISO } from 'date-fns';
import { ru } from 'date-fns/locale';

/**
 * Formatting helpers.
 *
 * The API returns UTC ISO strings. These render them in the browser's local zone,
 * which is what a user on the shop floor expects to see.
 */

export function formatDate(value: string | Date | null | undefined): string {
  if (!value) {
    return '—';
  }

  try {
    const date = typeof value === 'string' ? parseISO(value) : value;
    return format(date, 'dd.MM.yyyy', { locale: ru });
  } catch {
    return '—';
  }
}

export function formatDateTime(value: string | Date | null | undefined): string {
  if (!value) {
    return '—';
  }

  try {
    const date = typeof value === 'string' ? parseISO(value) : value;
    return format(date, 'dd.MM.yyyy HH:mm', { locale: ru });
  } catch {
    return '—';
  }
}

/** Percentage as stored by the API (already 0–100). */
export function formatPercent(value: number | null | undefined, digits = 2): string {
  if (value === null || value === undefined) {
    return '—';
  }

  return `${value.toFixed(digits).replace('.', ',')} %`;
}

export function formatNumber(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return '—';
  }

  // Thin spaces between thousands — the Russian convention.
  return value.toLocaleString('ru-RU');
}

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} Б`;
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} КБ`;
  }

  return `${(bytes / 1024 / 1024).toFixed(1)} МБ`;
}

/** Date for an <input type="date">, which requires yyyy-MM-dd. */
export function toInputDate(value: string | Date | null | undefined): string {
  if (!value) {
    return '';
  }

  try {
    const date = typeof value === 'string' ? parseISO(value) : value;
    return format(date, 'yyyy-MM-dd');
  } catch {
    return '';
  }
}
