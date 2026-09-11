import { beforeEach, describe, expect, it } from 'vitest'

import { mockBlacklistService } from '@/mocks/blacklist'
import { resetMockBusinessSnapshot } from '@/mocks/repositories/business-repository'
import { isServiceError, type UiErrorKind } from '@/types/api'

function loginAs(account: string) {
  window.sessionStorage.setItem(
    'training-management.mock-auth-session',
    JSON.stringify({ account, expiresAt: Date.now() + 3600_000 }),
  )
}

async function expectError(promise: Promise<unknown>, kind: UiErrorKind) {
  try {
    await promise
  } catch (error) {
    if (isServiceError(error)) {
      expect(error.ui.kind).toBe(kind)
      return
    }
    throw error
  }
  throw new Error('期望抛出服务错误，但请求成功返回。')
}

describe('黑名单 mock 服务', () => {
  beforeEach(() => {
    window.sessionStorage.clear()
    resetMockBusinessSnapshot()
  })

  it('主管仅能查看本部门黑名单', async () => {
    loginAs('manager.demo')
    const result = await mockBlacklistService.listBlacklists({ page: 1, pageSize: 20 })
    expect(result.total).toBe(1)
    expect(result.items[0]?.empId).toBe(55)
    expect(result.items[0]?.deptName).toBe('技术部')
  })

  it('管理员可查看全部黑名单', async () => {
    loginAs('admin.demo')
    const result = await mockBlacklistService.listBlacklists({ page: 1, pageSize: 20 })
    expect(result.total).toBe(2)
  })

  it('主管可将本部门员工加入黑名单并记录操作人', async () => {
    loginAs('manager.demo')
    const record = await mockBlacklistService.createBlacklist({ empId: 58, reason: '违规' })
    expect(record.empId).toBe(58)
    expect(record.deptName).toBe('技术部')
    expect(record.operatorEmpId).toBe(56)
    expect(record.status).toBe('ACTIVE')
  })

  it('主管不能跨部门加入黑名单', async () => {
    loginAs('manager.demo')
    await expectError(
      mockBlacklistService.createBlacklist({ empId: 60, reason: '违规' }),
      'forbidden',
    )
  })

  it('不能将自己加入黑名单', async () => {
    loginAs('manager.demo')
    await expectError(
      mockBlacklistService.createBlacklist({ empId: 56, reason: '违规' }),
      'validation',
    )
  })

  it('重复生效黑名单返回冲突', async () => {
    loginAs('manager.demo')
    await expectError(
      mockBlacklistService.createBlacklist({ empId: 55, reason: '重复' }),
      'conflict',
    )
  })

  it('员工不存在返回 404', async () => {
    loginAs('manager.demo')
    await expectError(
      mockBlacklistService.createBlacklist({ empId: 9999, reason: '违规' }),
      'not-found',
    )
  })

  it('普通员工无权限', async () => {
    loginAs('employee.demo')
    await expectError(
      mockBlacklistService.createBlacklist({ empId: 55, reason: '违规' }),
      'forbidden',
    )
  })

  it('空原因被拒绝', async () => {
    loginAs('manager.demo')
    await expectError(
      mockBlacklistService.createBlacklist({ empId: 58, reason: '   ' }),
      'validation',
    )
  })

  it('管理员可跨部门加入黑名单', async () => {
    loginAs('admin.demo')
    const record = await mockBlacklistService.createBlacklist({ empId: 58, reason: '违规' })
    expect(record.empId).toBe(58)
    expect(record.operatorEmpId).toBe(54)
  })
})
