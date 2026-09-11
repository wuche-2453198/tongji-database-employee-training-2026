import { requestApi } from '@/api/client'
import { mapEmployee, mapEmployeePage } from '@/api/mappers/employee'
import type { ApiEnvelopeDto, EmployeeSummaryDto, PageDto } from '@/api/transport'
import type { EmployeeService } from '@/services/employee'

export const httpEmployeeService: EmployeeService = {
  async listEmployees(query, options) {
    const response = await requestApi<ApiEnvelopeDto<PageDto<EmployeeSummaryDto>>>({
      method: 'GET',
      url: '/api/employees',
      params: {
        Keyword: query.keyword || undefined,
        DeptName: query.deptName || undefined,
        Status: query.status || undefined,
        Page: query.page,
        PageSize: query.pageSize,
      },
      signal: options?.signal,
      operation: 'query',
    })
    return mapEmployeePage(response)
  },

  async getEmployee(id, options) {
    const response = await requestApi<ApiEnvelopeDto<EmployeeSummaryDto>>({
      method: 'GET', url: `/api/employees/${encodeURIComponent(id)}`, signal: options?.signal, operation: 'query',
    })
    return mapEmployee(response)
  },

  async createEmployee(input, options) {
    const response = await requestApi<ApiEnvelopeDto<EmployeeSummaryDto>>({
      method: 'POST', url: '/api/employees', signal: options?.signal, operation: 'write',
      data: { LoginName: input.loginName, EmpName: input.empName, DeptName: input.deptName,
        Position: input.position || undefined, Email: input.email || undefined, Phone: input.phone || undefined,
        HireDate: input.hireDate, Password: input.password },
    })
    return mapEmployee(response)
  },

  async updateEmployee(id, input, options) {
    const response = await requestApi<ApiEnvelopeDto<EmployeeSummaryDto>>({
      method: 'PUT', url: `/api/employees/${encodeURIComponent(id)}`, signal: options?.signal, operation: 'write',
      data: { EmpName: input.empName, DeptName: input.deptName, Position: input.position || undefined,
        Email: input.email || undefined, Phone: input.phone || undefined, HireDate: input.hireDate || undefined,
        Status: input.status },
    })
    return mapEmployee(response)
  },

  async deleteEmployee(id, options) {
    await requestApi<ApiEnvelopeDto<boolean>>({
      method: 'DELETE', url: `/api/employees/${encodeURIComponent(id)}`, signal: options?.signal, operation: 'write',
    })
  },
}
