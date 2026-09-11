import type { BlacklistRecord, BlacklistService } from '@/domains/blacklist'
import { domainError } from '@/domains/errors'
import { getMockActor, mockBusinessRepository } from '@/mocks/repositories/business-repository'
import { createMockError, currentMockScenario, mockWait } from '@/mocks/scenarios'

const MaxReasonLength = 500

export const mockBlacklistService: BlacklistService = {
  async listBlacklists(query, options) {
    await mockWait(options?.signal)
    const scenario = currentMockScenario()
    if (['failure', 'forbidden', 'not-found'].includes(scenario)) throw createMockError(scenario)

    const actor = getMockActor()
    const records = mockBusinessRepository.getState().blacklist
    const scoped =
      actor.role === 'ADMIN' || actor.role === 'HR'
        ? records
        : records.filter((record) => record.deptName === actor.departmentName)

    const filtered = scoped.filter(
      (record) => !query.status || query.status === 'UNKNOWN' || record.status === query.status,
    )

    const start = (query.page - 1) * query.pageSize
    return {
      items: filtered.slice(start, start + query.pageSize),
      page: query.page,
      pageSize: query.pageSize,
      total: filtered.length,
    }
  },

  async createBlacklist(input, options) {
    await mockWait(options?.signal)
    const scenario = currentMockScenario()
    if (['failure', 'forbidden', 'not-found'].includes(scenario)) throw createMockError(scenario)

    const actor = getMockActor()
    if (actor.role !== 'ADMIN' && actor.role !== 'DEPT_MANAGER') {
      throw domainError('BLACKLIST_FORBIDDEN', {
        message: '只有管理员或部门主管可以将员工加入黑名单。',
      })
    }

    const reason = input.reason?.trim() ?? ''
    if (!reason) throw domainError('BLACKLIST_REASON_REQUIRED')
    if (reason.length > MaxReasonLength) throw domainError('BLACKLIST_REASON_TOO_LONG')

    if (actor.employeeId === input.empId) throw domainError('BLACKLIST_SELF')

    const state = mockBusinessRepository.getState()
    const target = state.employees.find((employee) => employee.empId === input.empId)
    if (!target) throw domainError('BLACKLIST_EMPLOYEE_NOT_FOUND')

    if (actor.role === 'DEPT_MANAGER' && target.deptName !== actor.departmentName) {
      throw domainError('BLACKLIST_FORBIDDEN', { message: '只能将本部门员工加入黑名单。' })
    }

    const duplicate = state.blacklist.some(
      (record) => record.empId === input.empId && record.status === 'ACTIVE',
    )
    if (duplicate) throw domainError('BLACKLIST_DUPLICATE')

    const record: BlacklistRecord = {
      id: String(Date.now()),
      empId: target.empId,
      empName: target.empName,
      deptName: target.deptName,
      reason,
      startDate: input.startDate ?? new Date().toISOString().slice(0, 10),
      endDate: input.endDate ?? null,
      status: 'ACTIVE',
      statusLabel: '生效中',
      operatorEmpId: actor.employeeId,
      createdAt: new Date().toISOString(),
    }

    mockBusinessRepository.update((next) => {
      next.blacklist.unshift(record)
    })

    return record
  },
}
