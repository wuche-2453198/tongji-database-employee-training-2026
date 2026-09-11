import type { DomainPage, DomainPageQuery, ServiceRequestOptions } from '@/domains/shared'

export interface DepartmentBudgetQuery extends DomainPageQuery {
  keyword?: string
  minBudget?: number
  maxBudget?: number
}

export interface DepartmentBudget {
  id: string
  departmentName: string
  annualBudget: number
  usedBudget: number
  remainingBudget: number
}

export interface DepartmentBudgetInput {
  departmentName: string
  annualBudget: number
}

export interface DepartmentBudgetService {
  list(query: DepartmentBudgetQuery, options?: ServiceRequestOptions): Promise<DomainPage<DepartmentBudget>>
  create(input: DepartmentBudgetInput, options?: ServiceRequestOptions): Promise<DepartmentBudget>
  update(id: string, input: DepartmentBudgetInput, options?: ServiceRequestOptions): Promise<DepartmentBudget>
  delete(id: string, options?: ServiceRequestOptions): Promise<void>
}
