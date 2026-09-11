import { requestApi } from '@/api/client'
import { mapEmployeePage } from '@/api/mappers/employee'
import type { ApiEnvelopeDto, EmployeeSummaryDto, PageDto } from '@/api/transport'
import type { EmployeeService } from '@/services/employee'

export const httpEmployeeService: EmployeeService = {
  async listEmployees(query, options) {
    const response = await requestApi<ApiEnvelopeDto<PageDto<EmployeeSummaryDto>>>({
      method: 'GET',
      url: '/api/employees',
      params: {
        Page: query.page,
        PageSize: query.pageSize,
      },
      signal: options?.signal,
      operation: 'query',
    })
    return mapEmployeePage(response)
  },
}
