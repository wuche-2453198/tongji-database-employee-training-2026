import { beforeEach, describe, expect, it } from 'vitest'

import { getActiveMenuKey, getNavigationForRoles } from '@/config/navigation'
import { getPermissionsForRoles, hasPermission } from '@/config/permissions'
import { pinia } from '@/stores'
import { useAuthStore } from '@/stores/auth'
import { isAuthServiceError, type AppRole, type AppUser } from '@/types/auth'

const authStore = useAuthStore(pinia)

const resetStore = () => {
  window.sessionStorage.clear()
  authStore.$patch({
    currentUser: null,
    status: 'idle',
    initialized: false,
    lastErrorCode: null,
    lastSessionEvent: null,
  })
}

describe('M4 Mock 认证与权限', () => {
  beforeEach(resetStore)

  const accounts: Array<{ account: string; role: AppRole; name: string }> = [
    { account: 'employee.demo', role: 'EMPLOYEE', name: '张三' },
    { account: 'manager.demo', role: 'MANAGER', name: '李主管' },
    { account: 'hr.demo', role: 'HR', name: '王 HR' },
    { account: 'admin.demo', role: 'ADMIN', name: '赵管理员' },
  ]

  it.each(accounts)('$account 可登录并恢复 $role 当前用户', async ({ account, role, name }) => {
    const user = await authStore.login({ account, password: 'Demo@123' })
    expect(user.primaryRole).toBe(role)
    expect(user.displayName).toBe(name)

    authStore.$patch({ currentUser: null, status: 'idle', initialized: false })
    await authStore.initialize()
    expect(authStore.currentUser?.account).toBe(account)
    expect(authStore.isAuthenticated).toBe(true)
  })

  it('退出同时清理 Store 与会话', async () => {
    await authStore.login({ account: 'employee.demo', password: 'Demo@123' })
    await authStore.logout()

    expect(authStore.currentUser).toBeNull()
    expect(authStore.isAuthenticated).toBe(false)
    expect(window.sessionStorage.getItem('training-management.mock-auth-session')).toBeNull()
  })

  it.each([
    ['employee.demo', 'wrong-password', 'INVALID_CREDENTIALS'],
    ['disabled.demo', 'Demo@123', 'ACCOUNT_DISABLED'],
    ['service-error.demo', 'Demo@123', 'SERVICE_UNAVAILABLE'],
  ] as const)('账号异常映射为 %s / %s', async (account, password, expectedCode) => {
    try {
      await authStore.login({ account, password })
      throw new Error('预期登录失败')
    } catch (error) {
      expect(isAuthServiceError(error)).toBe(true)
      if (isAuthServiceError(error)) expect(error.code).toBe(expectedCode)
      expect(authStore.lastErrorCode).toBe(expectedCode)
    }
  })

  it('过期会话被清除并登记过期事件', async () => {
    window.sessionStorage.setItem(
      'training-management.mock-auth-session',
      JSON.stringify({ account: 'employee.demo', expiresAt: Date.now() - 1 }),
    )

    await authStore.initialize(true)
    expect(authStore.currentUser).toBeNull()
    expect(authStore.lastSessionEvent).toBe('expired')
    expect(window.sessionStorage.getItem('training-management.mock-auth-session')).toBeNull()
  })

  it('多角色占位按权限并集生成菜单和操作权限', () => {
    const roles: AppRole[] = ['EMPLOYEE', 'MANAGER']
    const user: AppUser = {
      account: 'multi-role.test',
      displayName: '多角色占位',
      employeeId: 9998,
      departmentId: 101,
      departmentName: '技术部',
      position: '主管',
      status: 'ACTIVE',
      roles,
      primaryRole: 'MANAGER',
      permissions: getPermissionsForRoles(roles),
    }
    const menuKeys = getNavigationForRoles(user.roles)
      .flatMap((group) => group.items)
      .map((item) => item.menuKey)

    expect(menuKeys).toContain('my-requests')
    expect(menuKeys).toContain('department-approval')
    expect(hasPermission(user, 'request:create')).toBe(true)
    expect(hasPermission(user, 'request:department:approve')).toBe(true)
    expect(hasPermission(user, 'request:hr:file')).toBe(false)
  })

  it('详情页按当前角色保持对应业务菜单选中态', () => {
    expect(getActiveMenuKey('request-detail', ['EMPLOYEE'])).toBe('my-requests')
    expect(getActiveMenuKey('request-detail', ['MANAGER'])).toBe('department-approval')
    expect(getActiveMenuKey('request-detail', ['HR'])).toBe('hr-filing')
    expect(getActiveMenuKey('registration-detail', ['ADMIN'])).toBe('attendance-management')
    expect(getActiveMenuKey('certificate-detail', ['HR'])).toBe('certificate-management')
  })
})
