import type { EmployeeService } from '@/domains/employee'
import { getMockActor, mockBusinessRepository } from '@/mocks/repositories/business-repository'
import { createMockError, currentMockScenario, mockWait } from '@/mocks/scenarios'

export const mockEmployeeService: EmployeeService = {
  async listEmployees(query, options) {
    await mockWait(options?.signal)
    const scenario = currentMockScenario()
    if (['failure', 'forbidden', 'not-found'].includes(scenario)) throw createMockError(scenario)

    const actor = getMockActor()
    const employees = mockBusinessRepository.getState().employees
    const scoped =
      actor.role === 'ADMIN' || actor.role === 'HR'
        ? employees
        : employees.filter((employee) => employee.deptName === actor.departmentName)

    const filtered = query.keyword
      ? scoped.filter((employee) =>
          `${employee.empName}${employee.empId}`.includes(query.keyword as string),
        )
      : scoped

    const start = (query.page - 1) * query.pageSize
    return {
      items: filtered.slice(start, start + query.pageSize),
      page: query.page,
      pageSize: query.pageSize,
      total: filtered.length,
    }
  },
}
