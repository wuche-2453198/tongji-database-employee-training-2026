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

  async getEmployee(id, options) {
    await mockWait(options?.signal)
    const employee = mockBusinessRepository.getState().employees.find((item) => String(item.empId) === id)
    if (!employee) throw createMockError('not-found')
    return employee
  },

  async createEmployee(input, options) {
    await mockWait(options?.signal)
    const id = Math.max(...mockBusinessRepository.getState().employees.map((item) => item.empId), 0) + 1
    const employee = { empId: id, empName: input.empName.trim(), deptName: input.deptName.trim(),
      position: input.position?.trim() || '—', email: input.email?.trim() || null, phone: input.phone?.trim() || null,
      hireDate: input.hireDate, status: 'ACTIVE' as const, createdAt: new Date().toISOString() }
    mockBusinessRepository.update((next) => { next.employees.unshift(employee) })
    return employee
  },

  async updateEmployee(id, input, options) {
    await mockWait(options?.signal)
    let updated: import('@/domains/employee').EmployeeSummary | undefined
    mockBusinessRepository.update((next) => {
      const employee = next.employees.find((item) => String(item.empId) === id)
      if (!employee) return
      Object.assign(employee, { empName: input.empName.trim(), deptName: input.deptName.trim(),
        position: input.position?.trim() || '—', email: input.email?.trim() || null, phone: input.phone?.trim() || null,
        hireDate: input.hireDate || null, status: input.status })
      updated = employee
    })
    if (!updated) throw createMockError('not-found')
    return updated
  },

  async deleteEmployee(id, options) {
    await mockWait(options?.signal)
    const exists = mockBusinessRepository.getState().employees.some((item) => String(item.empId) === id)
    if (!exists) throw createMockError('not-found')
    mockBusinessRepository.update((next) => {
      const employee = next.employees.find((item) => String(item.empId) === id)
      if (employee) employee.status = 'RESIGNED'
    })
  },
}
