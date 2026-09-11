import type { BlacklistRecord, BlacklistService } from '@/domains/blacklist'
import { domainError } from '@/domains/errors'
import {
  getMockActor,
  getMockPrivilegedEmployeeIds,
  mockBusinessRepository,
} from '@/mocks/repositories/business-repository'
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

  async listCandidates(query, options) {
    await mockWait(options?.signal)
    const scenario = currentMockScenario()
    if (['failure', 'forbidden', 'not-found'].includes(scenario)) throw createMockError(scenario)

    const actor = getMockActor()
    if (actor.role !== 'ADMIN' && actor.role !== 'HR' && actor.role !== 'DEPT_MANAGER') {
      throw domainError('BLACKLIST_FORBIDDEN', { message: '无权查看黑名单候选员工。' })
    }

    const privileged = getMockPrivilegedEmployeeIds()
    const keyword = query.keyword?.trim().toLowerCase()

    const candidates = mockBusinessRepository
      .getState()
      .employees.filter((employee) => {
        if (employee.status !== 'ACTIVE') return false
        if (privileged.has(employee.empId)) return false
        if (actor.role === 'DEPT_MANAGER' && employee.deptName !== actor.departmentName) {
          return false
        }
        if (
          keyword &&
          !employee.empName.toLowerCase().includes(keyword) &&
          !String(employee.empId).includes(keyword)
        ) {
          return false
        }
        return true
      })
      .map((employee) => ({
        empId: employee.empId,
        empName: employee.empName,
        deptName: employee.deptName,
        position: employee.position,
        status: employee.status,
      }))

    return {
      items: candidates,
      page: query.page,
      pageSize: query.pageSize,
      total: candidates.length,
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

    const today = localIsoDate(new Date())
    if (input.endDate && input.endDate <= today) {
      throw domainError('BLACKLIST_END_DATE_PAST')
    }
    if (input.startDate && input.endDate && input.endDate < input.startDate) {
      throw domainError('BLACKLIST_DATE_RANGE_INVALID')
    }

    const state = mockBusinessRepository.getState()
    const target = state.employees.find((employee) => employee.empId === input.empId)
    if (!target) throw domainError('BLACKLIST_EMPLOYEE_NOT_FOUND')

    if (actor.role === 'DEPT_MANAGER' && target.deptName !== actor.departmentName) {
      throw domainError('BLACKLIST_FORBIDDEN', { message: '只能将本部门员工加入黑名单。' })
    }

    if (actor.role === 'DEPT_MANAGER' && getMockPrivilegedEmployeeIds().has(target.empId)) {
      throw domainError('BLACKLIST_FORBIDDEN', {
        message: '不能将管理员、HR 或部门主管加入黑名单。',
      })
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
      startDate: input.startDate ?? localIsoDate(new Date()),
      endDate: input.endDate ?? null,
      status: 'ACTIVE',
      statusLabel: '生效中',
      createdAt: new Date().toISOString(),
    }

    mockBusinessRepository.update((next) => {
      next.blacklist.unshift(record)
    })

    return record
  },

  async updateBlacklist(id, input, options) {
    await mockWait(options?.signal)
    const actor = getMockActor()
    if (actor.role !== 'ADMIN') throw domainError('BLACKLIST_FORBIDDEN')
    let updated: BlacklistRecord | undefined
    mockBusinessRepository.update((next) => {
      const record = next.blacklist.find((item) => item.id === id)
      if (!record) return
      if (input.reason !== undefined) record.reason = input.reason.trim()
      if (input.endDate !== undefined) record.endDate = input.endDate
      if (input.status !== undefined) {
        record.status = input.status
        record.statusLabel = input.status === 'ACTIVE' ? '生效中' : '已解除'
      }
      updated = record
    })
    if (!updated) throw createMockError('not-found')
    return updated
  },

  async deleteBlacklist(id, options) {
    await mockWait(options?.signal)
    if (getMockActor().role !== 'ADMIN') throw domainError('BLACKLIST_FORBIDDEN')
    const exists = mockBusinessRepository.getState().blacklist.some((item) => item.id === id)
    if (!exists) throw createMockError('not-found')
    mockBusinessRepository.update((next) => {
      next.blacklist = next.blacklist.filter((item) => item.id !== id)
    })
  },
}

function localIsoDate(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}
