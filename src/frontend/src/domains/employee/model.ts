import type { DomainPage, DomainPageQuery, ServiceRequestOptions } from '@/domains/shared'

export interface EmployeeQuery extends DomainPageQuery {
  keyword?: string
}

export interface EmployeeSummary {
  empId: number
  empName: string
  deptName: string
  position: string
}

export interface EmployeeService {
  listEmployees(
    query: EmployeeQuery,
    options?: ServiceRequestOptions,
  ): Promise<DomainPage<EmployeeSummary>>
}
