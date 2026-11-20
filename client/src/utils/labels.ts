import type {
  AuditAction,
  BatchStatus,
  CorrectiveActionStatus,
  DefectSeverity,
  InspectionOutcome,
  UserRole,
} from '../types/api';

/**
 * Russian captions and colours for the API's enums.
 *
 * The API sends enum names, not display text, on purpose: the wire format stays
 * stable while wording changes here. MUI palette keys are used rather than raw
 * hex so the chips follow the theme.
 */

type ChipColour = 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning';

export const batchStatusLabels: Record<BatchStatus, string> = {
  InProduction: 'В производстве',
  AwaitingInspection: 'Ожидает контроля',
  UnderInspection: 'На контроле',
  Accepted: 'Принята',
  ReworkRequired: 'Требуется доработка',
  Rejected: 'Забракована',
  Closed: 'Закрыта',
};

export const batchStatusColours: Record<BatchStatus, ChipColour> = {
  InProduction: 'default',
  AwaitingInspection: 'info',
  UnderInspection: 'primary',
  Accepted: 'success',
  ReworkRequired: 'warning',
  Rejected: 'error',
  Closed: 'default',
};

export const inspectionOutcomeLabels: Record<InspectionOutcome, string> = {
  Conforming: 'Соответствует требованиям',
  ReworkRequired: 'Требуется доработка',
  Defective: 'Брак',
};

export const inspectionOutcomeColours: Record<InspectionOutcome, ChipColour> = {
  Conforming: 'success',
  ReworkRequired: 'warning',
  Defective: 'error',
};

export const defectSeverityLabels: Record<DefectSeverity, string> = {
  Minor: 'Незначительный',
  Major: 'Значительный',
  Critical: 'Критический',
};

export const defectSeverityColours: Record<DefectSeverity, ChipColour> = {
  Minor: 'info',
  Major: 'warning',
  Critical: 'error',
};

export const correctiveActionStatusLabels: Record<CorrectiveActionStatus, string> = {
  New: 'Новый',
  Assigned: 'Назначен',
  InProgress: 'В работе',
  Fixed: 'Исправлен',
  Verified: 'Проверен',
  Closed: 'Закрыт',
};

export const correctiveActionStatusColours: Record<CorrectiveActionStatus, ChipColour> = {
  New: 'default',
  Assigned: 'info',
  InProgress: 'primary',
  Fixed: 'secondary',
  Verified: 'success',
  Closed: 'default',
};

export const roleLabels: Record<UserRole, string> = {
  Administrator: 'Администратор',
  QualityInspector: 'Контролёр ОТК',
  WorkshopManager: 'Начальник участка',
  ProductionManager: 'Руководитель производства',
};

export const auditActionLabels: Record<AuditAction, string> = {
  Login: 'Вход',
  LoginFailed: 'Неудачный вход',
  Logout: 'Выход',
  Create: 'Создание',
  Update: 'Изменение',
  Delete: 'Удаление',
  Archive: 'Архивирование',
  Restore: 'Восстановление',
  Inspection: 'Проверка',
  DefectRegistered: 'Регистрация дефекта',
  CorrectiveActionStatusChanged: 'Смена статуса КД',
  PasswordResetRequested: 'Запрос сброса пароля',
  PasswordResetCompleted: 'Сброс пароля',
  Export: 'Экспорт',
};

/** Options for a select, from a label map. */
export function toOptions<T extends string>(labels: Record<T, string>): { value: T; label: string }[] {
  return (Object.keys(labels) as T[]).map((value) => ({ value, label: labels[value] }));
}
