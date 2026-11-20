import { apiClient, downloadFile } from './client';
import type {
  AuthResponse,
  AuditLogEntry,
  AuditLogQuery,
  CorrectiveAction,
  CorrectiveActionQuery,
  CorrectiveActionStatus,
  CurrentUser,
  Dashboard,
  Defect,
  DefectPhoto,
  DefectQuery,
  DefectType,
  Department,
  DepartmentStatistics,
  DefectTypeStatistics,
  Employee,
  EmployeeQuery,
  MonthlyTrendPoint,
  PagedResult,
  Product,
  ProductCategory,
  ProductQuery,
  ProductionBatch,
  ProductionBatchQuery,
  QualityInspection,
  QualityInspectionQuery,
  ReportPeriodQuery,
  Role,
  User,
  UserQuery,
  BatchStatus,
  CreateQualityInspectionRequest,
} from '../types/api';

/**
 * The API surface, one function per endpoint.
 *
 * Query objects go through `params`, and Axios drops undefined keys, so optional
 * filters simply do not appear in the URL.
 */

export const authApi = {
  login: (userName: string, password: string) =>
    apiClient.post<AuthResponse>('/auth/login', { userName, password }).then((r) => r.data),

  logout: (refreshToken: string) => apiClient.post('/auth/logout', { refreshToken }),

  me: () => apiClient.get<CurrentUser>('/auth/me').then((r) => r.data),

  forgotPassword: (email: string) => apiClient.post('/auth/forgot-password', { email }),

  resetPassword: (email: string, token: string, newPassword: string) =>
    apiClient.post('/auth/reset-password', { email, token, newPassword }),

  changePassword: (currentPassword: string, newPassword: string) =>
    apiClient.post('/auth/change-password', { currentPassword, newPassword }),
};

export const productsApi = {
  getPaged: (params: ProductQuery) =>
    apiClient.get<PagedResult<Product>>('/products', { params }).then((r) => r.data),

  getById: (id: number) => apiClient.get<Product>(`/products/${id}`).then((r) => r.data),

  create: (data: Partial<Product>) => apiClient.post<Product>('/products', data).then((r) => r.data),

  update: (id: number, data: Partial<Product>) =>
    apiClient.put<Product>(`/products/${id}`, data).then((r) => r.data),

  remove: (id: number) => apiClient.delete(`/products/${id}`),

  archive: (id: number) => apiClient.post<Product>(`/products/${id}/archive`).then((r) => r.data),

  restore: (id: number) => apiClient.post<Product>(`/products/${id}/restore`).then((r) => r.data),
};

export const categoriesApi = {
  getPaged: (params: { page?: number; pageSize?: number; search?: string }) =>
    apiClient.get<PagedResult<ProductCategory>>('/product-categories', { params }).then((r) => r.data),

  lookup: () => apiClient.get<ProductCategory[]>('/product-categories/lookup').then((r) => r.data),

  create: (data: Partial<ProductCategory>) =>
    apiClient.post<ProductCategory>('/product-categories', data).then((r) => r.data),

  update: (id: number, data: Partial<ProductCategory>) =>
    apiClient.put<ProductCategory>(`/product-categories/${id}`, data).then((r) => r.data),

  remove: (id: number) => apiClient.delete(`/product-categories/${id}`),
};

export const departmentsApi = {
  getPaged: (params: { page?: number; pageSize?: number; search?: string; isActive?: boolean }) =>
    apiClient.get<PagedResult<Department>>('/departments', { params }).then((r) => r.data),

  lookup: () => apiClient.get<Department[]>('/departments/lookup').then((r) => r.data),

  create: (data: Partial<Department>) =>
    apiClient.post<Department>('/departments', data).then((r) => r.data),

  update: (id: number, data: Partial<Department>) =>
    apiClient.put<Department>(`/departments/${id}`, data).then((r) => r.data),

  remove: (id: number) => apiClient.delete(`/departments/${id}`),
};

export const employeesApi = {
  getPaged: (params: EmployeeQuery) =>
    apiClient.get<PagedResult<Employee>>('/employees', { params }).then((r) => r.data),

  lookup: (departmentId?: number) =>
    apiClient.get<Employee[]>('/employees/lookup', { params: { departmentId } }).then((r) => r.data),

  getById: (id: number) => apiClient.get<Employee>(`/employees/${id}`).then((r) => r.data),

  create: (data: Partial<Employee>) => apiClient.post<Employee>('/employees', data).then((r) => r.data),

  update: (id: number, data: Partial<Employee>) =>
    apiClient.put<Employee>(`/employees/${id}`, data).then((r) => r.data),

  remove: (id: number) => apiClient.delete(`/employees/${id}`),
};

export const defectTypesApi = {
  getPaged: (params: { page?: number; pageSize?: number; search?: string; isActive?: boolean }) =>
    apiClient.get<PagedResult<DefectType>>('/defect-types', { params }).then((r) => r.data),

  lookup: () => apiClient.get<DefectType[]>('/defect-types/lookup').then((r) => r.data),

  create: (data: Partial<DefectType>) =>
    apiClient.post<DefectType>('/defect-types', data).then((r) => r.data),

  update: (id: number, data: Partial<DefectType>) =>
    apiClient.put<DefectType>(`/defect-types/${id}`, data).then((r) => r.data),

  remove: (id: number) => apiClient.delete(`/defect-types/${id}`),
};

export const batchesApi = {
  getPaged: (params: ProductionBatchQuery) =>
    apiClient.get<PagedResult<ProductionBatch>>('/production-batches', { params }).then((r) => r.data),

  getInspectable: () =>
    apiClient.get<ProductionBatch[]>('/production-batches/inspectable').then((r) => r.data),

  getById: (id: number) =>
    apiClient.get<ProductionBatch>(`/production-batches/${id}`).then((r) => r.data),

  create: (data: Partial<ProductionBatch>) =>
    apiClient.post<ProductionBatch>('/production-batches', data).then((r) => r.data),

  update: (id: number, data: Partial<ProductionBatch>) =>
    apiClient.put<ProductionBatch>(`/production-batches/${id}`, data).then((r) => r.data),

  changeStatus: (id: number, status: BatchStatus, comment?: string) =>
    apiClient
      .post<ProductionBatch>(`/production-batches/${id}/status`, { status, comment })
      .then((r) => r.data),

  remove: (id: number) => apiClient.delete(`/production-batches/${id}`),
};

export const inspectionsApi = {
  getPaged: (params: QualityInspectionQuery) =>
    apiClient.get<PagedResult<QualityInspection>>('/inspections', { params }).then((r) => r.data),

  getById: (id: number) => apiClient.get<QualityInspection>(`/inspections/${id}`).then((r) => r.data),

  create: (data: CreateQualityInspectionRequest) =>
    apiClient.post<QualityInspection>('/inspections', data).then((r) => r.data),

  update: (id: number, data: { inspectionDate: string; comment?: string; results: unknown[] }) =>
    apiClient.put<QualityInspection>(`/inspections/${id}`, data).then((r) => r.data),

  remove: (id: number) => apiClient.delete(`/inspections/${id}`),
};

export const defectsApi = {
  getPaged: (params: DefectQuery) =>
    apiClient.get<PagedResult<Defect>>('/defects', { params }).then((r) => r.data),

  getById: (id: number) => apiClient.get<Defect>(`/defects/${id}`).then((r) => r.data),

  create: (data: Partial<Defect> & { qualityInspectionId: number }) =>
    apiClient.post<Defect>('/defects', data).then((r) => r.data),

  update: (id: number, data: Partial<Defect>) =>
    apiClient.put<Defect>(`/defects/${id}`, data).then((r) => r.data),

  remove: (id: number) => apiClient.delete(`/defects/${id}`),

  uploadPhoto: (defectId: number, file: File, caption?: string) => {
    const form = new FormData();
    form.append('file', file);

    if (caption) {
      form.append('caption', caption);
    }

    // Content-Type is left unset on purpose: the browser must add the multipart
    // boundary itself, and naming the type here would omit it.
    return apiClient
      .post<DefectPhoto>(`/defects/${defectId}/photos`, form, {
        headers: { 'Content-Type': undefined },
      })
      .then((r) => r.data);
  },

  deletePhoto: (defectId: number, photoId: number) =>
    apiClient.delete(`/defects/${defectId}/photos/${photoId}`),
};

export const correctiveActionsApi = {
  getPaged: (params: CorrectiveActionQuery) =>
    apiClient.get<PagedResult<CorrectiveAction>>('/corrective-actions', { params }).then((r) => r.data),

  getById: (id: number) =>
    apiClient.get<CorrectiveAction>(`/corrective-actions/${id}`).then((r) => r.data),

  create: (data: { defectId: number; description: string; assignedToId?: number; dueDate?: string }) =>
    apiClient.post<CorrectiveAction>('/corrective-actions', data).then((r) => r.data),

  update: (id: number, data: { description: string; dueDate?: string; resolutionNotes?: string }) =>
    apiClient.put<CorrectiveAction>(`/corrective-actions/${id}`, data).then((r) => r.data),

  assign: (id: number, assignedToId: number, comment?: string) =>
    apiClient
      .post<CorrectiveAction>(`/corrective-actions/${id}/assign`, { assignedToId, comment })
      .then((r) => r.data),

  changeStatus: (id: number, status: CorrectiveActionStatus, comment?: string) =>
    apiClient
      .post<CorrectiveAction>(`/corrective-actions/${id}/status`, { status, comment })
      .then((r) => r.data),
};

export const usersApi = {
  getPaged: (params: UserQuery) =>
    apiClient.get<PagedResult<User>>('/users', { params }).then((r) => r.data),

  getRoles: () => apiClient.get<Role[]>('/users/roles').then((r) => r.data),

  getById: (id: number) => apiClient.get<User>(`/users/${id}`).then((r) => r.data),

  create: (data: { userName: string; email: string; password: string; roleId: number; employeeId?: number }) =>
    apiClient.post<User>('/users', data).then((r) => r.data),

  update: (id: number, data: { email: string; roleId: number; employeeId?: number; isActive: boolean }) =>
    apiClient.put<User>(`/users/${id}`, data).then((r) => r.data),

  resetPassword: (id: number, newPassword: string) =>
    apiClient.post(`/users/${id}/reset-password`, { newPassword }),

  unlock: (id: number) => apiClient.post(`/users/${id}/unlock`),

  remove: (id: number) => apiClient.delete(`/users/${id}`),
};

export const reportsApi = {
  getDashboard: (params: ReportPeriodQuery) =>
    apiClient.get<Dashboard>('/reports/dashboard', { params }).then((r) => r.data),

  getDepartmentStatistics: (params: ReportPeriodQuery) =>
    apiClient.get<DepartmentStatistics[]>('/reports/departments', { params }).then((r) => r.data),

  getTopDefectTypes: (params: ReportPeriodQuery & { top?: number }) =>
    apiClient.get<DefectTypeStatistics[]>('/reports/top-defect-types', { params }).then((r) => r.data),

  getMonthlyTrend: (params: ReportPeriodQuery) =>
    apiClient.get<MonthlyTrendPoint[]>('/reports/monthly-trend', { params }).then((r) => r.data),

  /** Export endpoints stream a file; downloadFile carries the auth header and saves it. */
  exportInspections: (params: QualityInspectionQuery & { format: 'Excel' | 'Pdf' }) =>
    downloadFile('/reports/export/inspections', { params }),

  exportDefects: (params: DefectQuery & { format: 'Excel' | 'Pdf' }) =>
    downloadFile('/reports/export/defects', { params }),

  exportCorrectiveActions: (params: CorrectiveActionQuery & { format: 'Excel' | 'Pdf' }) =>
    downloadFile('/reports/export/corrective-actions', { params }),

  exportDepartments: (params: ReportPeriodQuery & { format: 'Excel' | 'Pdf' }) =>
    downloadFile('/reports/export/departments', { params }),

  exportTopDefectTypes: (params: ReportPeriodQuery & { format: 'Excel' | 'Pdf' }) =>
    downloadFile('/reports/export/top-defect-types', { params }),
};

export const auditApi = {
  getPaged: (params: AuditLogQuery) =>
    apiClient.get<PagedResult<AuditLogEntry>>('/audit-logs', { params }).then((r) => r.data),

  getEntityHistory: (entityName: string, entityId: string) =>
    apiClient.get<AuditLogEntry[]>(`/audit-logs/entity/${entityName}/${entityId}`).then((r) => r.data),
};
