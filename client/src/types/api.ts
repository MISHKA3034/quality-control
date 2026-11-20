/**
 * Types mirroring the API contract.
 *
 * Kept in step with the C# DTOs by hand. The enums must match the server's names
 * exactly: the API serialises enums as names (JsonStringEnumConverter), so a typo
 * here becomes a silent runtime mismatch rather than a compile error.
 */

export type UserRole =
  | 'Administrator'
  | 'QualityInspector'
  | 'WorkshopManager'
  | 'ProductionManager';

export type BatchStatus =
  | 'InProduction'
  | 'AwaitingInspection'
  | 'UnderInspection'
  | 'Accepted'
  | 'ReworkRequired'
  | 'Rejected'
  | 'Closed';

export type InspectionOutcome = 'Conforming' | 'ReworkRequired' | 'Defective';

export type DefectSeverity = 'Minor' | 'Major' | 'Critical';

export type CorrectiveActionStatus =
  | 'New'
  | 'Assigned'
  | 'InProgress'
  | 'Fixed'
  | 'Verified'
  | 'Closed';

export type AuditAction =
  | 'Login'
  | 'LoginFailed'
  | 'Logout'
  | 'Create'
  | 'Update'
  | 'Delete'
  | 'Archive'
  | 'Restore'
  | 'Inspection'
  | 'DefectRegistered'
  | 'CorrectiveActionStatusChanged'
  | 'PasswordResetRequested'
  | 'PasswordResetCompleted'
  | 'Export';

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface PagedQuery {
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

// ---------- Auth ----------

export interface CurrentUser {
  id: number;
  userName: string;
  email: string;
  role: UserRole;
  roleDisplayName: string;
  employeeId?: number;
  fullName?: string;
  position?: string;
  departmentId?: number;
  departmentName?: string;
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: CurrentUser;
}

// ---------- Reference data ----------

export interface ProductCategory {
  id: number;
  code: string;
  name: string;
  description?: string;
  parentCategoryId?: number;
  parentCategoryName?: string;
  productCount: number;
}

export interface Product {
  id: number;
  code: string;
  name: string;
  categoryId: number;
  categoryName: string;
  drawingNumber?: string;
  unitOfMeasure: string;
  isArchived: boolean;
  batchCount: number;
  description?: string;
  archivedAt?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface ProductQuery extends PagedQuery {
  search?: string;
  categoryId?: number;
  isArchived?: boolean;
}

export interface Department {
  id: number;
  code: string;
  name: string;
  description?: string;
  parentDepartmentId?: number;
  parentDepartmentName?: string;
  managerId?: number;
  managerName?: string;
  isActive: boolean;
  employeeCount: number;
}

export interface Employee {
  id: number;
  personnelNumber: string;
  lastName: string;
  firstName: string;
  middleName?: string;
  fullName: string;
  shortName: string;
  position: string;
  departmentId: number;
  departmentName: string;
  email?: string;
  phone?: string;
  hiredOn?: string;
  dismissedOn?: string;
  isActive: boolean;
  hasUserAccount: boolean;
}

export interface EmployeeQuery extends PagedQuery {
  search?: string;
  departmentId?: number;
  isActive?: boolean;
}

export interface DefectType {
  id: number;
  code: string;
  name: string;
  description?: string;
  defaultSeverity: DefectSeverity;
  isActive: boolean;
  defectCount: number;
}

// ---------- Production ----------

export interface ProductionBatch {
  id: number;
  batchNumber: string;
  productId: number;
  productName: string;
  productCode: string;
  manufactureDate: string;
  departmentId: number;
  departmentName: string;
  foremanId: number;
  foremanName: string;
  quantity: number;
  status: BatchStatus;
  inspectionCount: number;
  notes?: string;
  createdAt?: string;
  updatedAt?: string;
  allowedNextStatuses?: BatchStatus[];
}

export interface ProductionBatchQuery extends PagedQuery {
  search?: string;
  productId?: number;
  departmentId?: number;
  foremanId?: number;
  status?: BatchStatus;
  dateFrom?: string;
  dateTo?: string;
}

// ---------- Inspections ----------

export interface InspectionResultLine {
  id: number;
  parameterName: string;
  expectedValue?: string;
  actualValue?: string;
  unitOfMeasure?: string;
  outcome: InspectionOutcome;
  comment?: string;
  sortOrder: number;
}

export interface QualityInspection {
  id: number;
  inspectionNumber: string;
  productionBatchId: number;
  batchNumber: string;
  productName: string;
  departmentId: number;
  departmentName: string;
  inspectorId: number;
  inspectorName: string;
  inspectionDate: string;
  outcome: InspectionOutcome;
  quantityInspected: number;
  quantityAccepted: number;
  quantityRejected: number;
  defectCount: number;
  comment?: string;
  createdAt?: string;
  results?: InspectionResultLine[];
  defects?: Defect[];
}

export interface QualityInspectionQuery extends PagedQuery {
  search?: string;
  productionBatchId?: number;
  productId?: number;
  inspectorId?: number;
  departmentId?: number;
  outcome?: InspectionOutcome;
  dateFrom?: string;
  dateTo?: string;
  defectTypeId?: number;
}

export interface CreateInspectionResultRequest {
  parameterName: string;
  expectedValue?: string;
  actualValue?: string;
  unitOfMeasure?: string;
  outcome: InspectionOutcome;
  comment?: string;
  sortOrder: number;
}

export interface CreateInspectionDefectRequest {
  defectTypeId: number;
  description: string;
  severity: DefectSeverity;
  location?: string;
  comment?: string;
  affectedQuantity: number;
}

export interface CreateQualityInspectionRequest {
  productionBatchId: number;
  inspectionDate: string;
  outcome: InspectionOutcome;
  comment?: string;
  quantityInspected: number;
  quantityAccepted: number;
  quantityRejected: number;
  results: CreateInspectionResultRequest[];
  defects: CreateInspectionDefectRequest[];
}

// ---------- Defects ----------

export interface DefectPhoto {
  id: number;
  defectId: number;
  originalFileName: string;
  storedFileName: string;
  contentType: string;
  fileSizeBytes: number;
  caption?: string;
  uploadedAt: string;
  url: string;
}

export interface Defect {
  id: number;
  qualityInspectionId: number;
  inspectionNumber: string;
  productionBatchId: number;
  batchNumber: string;
  productName: string;
  departmentId: number;
  departmentName: string;
  defectTypeId: number;
  defectTypeName: string;
  description: string;
  severity: DefectSeverity;
  location?: string;
  affectedQuantity: number;
  detectedAt: string;
  photoCount: number;
  isOpen: boolean;
  correctiveActionCount: number;
  comment?: string;
  createdAt?: string;
  photos?: DefectPhoto[];
}

export interface DefectQuery extends PagedQuery {
  search?: string;
  qualityInspectionId?: number;
  productionBatchId?: number;
  productId?: number;
  defectTypeId?: number;
  departmentId?: number;
  inspectorId?: number;
  severity?: DefectSeverity;
  isOpen?: boolean;
  dateFrom?: string;
  dateTo?: string;
}

// ---------- Corrective actions ----------

export interface CorrectiveActionHistoryEntry {
  id: number;
  fromStatus?: CorrectiveActionStatus;
  toStatus: CorrectiveActionStatus;
  changedByUserId: number;
  changedByUserName: string;
  changedAt: string;
  comment?: string;
}

export interface CorrectiveAction {
  id: number;
  defectId: number;
  defectDescription: string;
  defectSeverity: DefectSeverity;
  defectTypeName: string;
  productionBatchId: number;
  batchNumber: string;
  productName: string;
  departmentId: number;
  departmentName: string;
  description: string;
  status: CorrectiveActionStatus;
  assignedToId?: number;
  assignedToName?: string;
  dueDate?: string;
  createdAt: string;
  closedAt?: string;
  isOverdue: boolean;
  assignedAt?: string;
  startedAt?: string;
  fixedAt?: string;
  verifiedAt?: string;
  verifiedByName?: string;
  resolutionNotes?: string;
  history?: CorrectiveActionHistoryEntry[];
  allowedNextStatuses?: CorrectiveActionStatus[];
}

export interface CorrectiveActionQuery extends PagedQuery {
  search?: string;
  defectId?: number;
  productionBatchId?: number;
  departmentId?: number;
  assignedToId?: number;
  status?: CorrectiveActionStatus;
  severity?: DefectSeverity;
  isOverdue?: boolean;
  dateFrom?: string;
  dateTo?: string;
}

// ---------- Users ----------

export interface Role {
  id: number;
  name: UserRole;
  displayName: string;
  description?: string;
}

export interface User {
  id: number;
  userName: string;
  email: string;
  roleId: number;
  roleName: UserRole;
  roleDisplayName: string;
  employeeId?: number;
  employeeName?: string;
  departmentName?: string;
  isActive: boolean;
  lastLoginAt?: string;
  isLockedOut: boolean;
  createdAt: string;
}

export interface UserQuery extends PagedQuery {
  search?: string;
  roleId?: number;
  isActive?: boolean;
}

// ---------- Reports ----------

export interface ReportPeriodQuery {
  dateFrom?: string;
  dateTo?: string;
  departmentId?: number;
  productId?: number;
}

export interface DashboardSummary {
  totalInspections: number;
  totalBatchesInspected: number;
  totalUnitsInspected: number;
  totalUnitsRejected: number;
  defectRatePercent: number;
  openDefects: number;
  criticalOpenDefects: number;
  closedCorrectiveActions: number;
  overdueCorrectiveActions: number;
  inspectionsConforming: number;
  inspectionsReworkRequired: number;
  inspectionsDefective: number;
}

export interface DepartmentStatistics {
  departmentId: number;
  departmentName: string;
  inspectionCount: number;
  unitsInspected: number;
  unitsRejected: number;
  defectRatePercent: number;
  defectCount: number;
  openDefectCount: number;
}

export interface DefectTypeStatistics {
  defectTypeId: number;
  defectTypeName: string;
  defectTypeCode: string;
  defectCount: number;
  affectedUnits: number;
  sharePercent: number;
  criticalCount: number;
}

export interface MonthlyTrendPoint {
  year: number;
  month: number;
  label: string;
  inspectionCount: number;
  unitsInspected: number;
  unitsRejected: number;
  defectRatePercent: number;
  defectCount: number;
}

export interface Dashboard {
  periodFrom: string;
  periodTo: string;
  summary: DashboardSummary;
  byDepartment: DepartmentStatistics[];
  topDefectTypes: DefectTypeStatistics[];
  monthlyTrend: MonthlyTrendPoint[];
}

// ---------- Audit ----------

export interface AuditLogEntry {
  id: number;
  userId?: number;
  userName?: string;
  action: AuditAction;
  entityName?: string;
  entityId?: string;
  description?: string;
  changes?: string;
  timestamp: string;
  ipAddress?: string;
  isSuccess: boolean;
}

export interface AuditLogQuery extends PagedQuery {
  search?: string;
  userId?: number;
  action?: AuditAction;
  entityName?: string;
  entityId?: string;
  isSuccess?: boolean;
  dateFrom?: string;
  dateTo?: string;
}

/** RFC 7807 problem response, as returned by the API's exception middleware. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}
