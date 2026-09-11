import { requestApi } from '@/api/client'
import { mapDepartmentBudget, mapDepartmentBudgetPage } from '@/api/mappers/budget'
import type { ApiEnvelopeDto, DepartmentBudgetDto, PageDto } from '@/api/transport'
import type { DepartmentBudgetService } from '@/domains/budget'

export const httpDepartmentBudgetService: DepartmentBudgetService = {
  async list(query, options) {
    const response = await requestApi<ApiEnvelopeDto<PageDto<DepartmentBudgetDto>>>({
      method: 'GET', url: '/api/department-trainings', signal: options?.signal, operation: 'query',
      params: { Keyword: query.keyword || undefined, MinBudget: query.minBudget, MaxBudget: query.maxBudget,
        Page: query.page, PageSize: query.pageSize, SortBy: 'DeptName', SortOrder: 'ASC' },
    })
    return mapDepartmentBudgetPage(response)
  },
  async create(input, options) {
    const response = await requestApi<ApiEnvelopeDto<DepartmentBudgetDto>>({
      method: 'POST', url: '/api/department-trainings', signal: options?.signal, operation: 'write',
      data: { DeptName: input.departmentName, AnnualBudget: input.annualBudget },
    })
    return mapDepartmentBudget(response)
  },
  async update(id, input, options) {
    const response = await requestApi<ApiEnvelopeDto<DepartmentBudgetDto>>({
      method: 'PUT', url: `/api/department-trainings/${encodeURIComponent(id)}`, signal: options?.signal, operation: 'write',
      data: { DeptName: input.departmentName, AnnualBudget: input.annualBudget },
    })
    return mapDepartmentBudget(response)
  },
  async delete(id, options) {
    await requestApi<ApiEnvelopeDto<boolean>>({ method: 'DELETE', url: `/api/department-trainings/${encodeURIComponent(id)}`, signal: options?.signal, operation: 'write' })
  },
}
