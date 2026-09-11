import type { DomainPage, DomainPageQuery, ServiceRequestOptions } from '@/domains/shared'

export interface EmployeeQuery extends DomainPageQuery {
  keyword?: string
  deptName?: string
  status?: 'ACTIVE' | 'RESIGNED'
}

export interface EmployeeSummary {
  empId: number
  empName: string
  deptName: string
  position: string
  email: string | null
  phone: string | null
  hireDate: string | null
  status: 'ACTIVE' | 'RESIGNED'
  createdAt: string | null
}

export interface CreateEmployeeInput {
  loginName: string
  empName: string
  deptName: string
  position?: string
  email?: string
  phone?: string
  hireDate: string
  password: string
}

export interface UpdateEmployeeInput {
  empName: string
  deptName: string
  position?: string
  email?: string
  phone?: string
  hireDate?: string
  status: 'ACTIVE' | 'RESIGNED'
}

export interface EmployeeService {
  listEmployees(
    query: EmployeeQuery,
    options?: ServiceRequestOptions,
  ): Promise<DomainPage<EmployeeSummary>>
  getEmployee(id: string, options?: ServiceRequestOptions): Promise<EmployeeSummary>
  createEmployee(input: CreateEmployeeInput, options?: ServiceRequestOptions): Promise<EmployeeSummary>
  updateEmployee(
    id: string,
    input: UpdateEmployeeInput,
    options?: ServiceRequestOptions,
  ): Promise<EmployeeSummary>
  deleteEmployee(id: string, options?: ServiceRequestOptions): Promise<void>
}
