import { describe, expect, it } from 'vitest'

import { getPermissionsForRoles } from '@/config/permissions'
import { getNavigationForRole } from '@/config/navigation'
import router, { resolveRouteAccess } from '@/router'
import type { AppRole, AppUser } from '@/types/auth'

const makeUser = (roles: AppRole[]): AppUser => ({
  account: 'route.test',
  displayName: '路由测试',
  employeeId: 9999,
  departmentId: null,
  departmentName: null,
  position: null,
  status: 'ACTIVE',
  roles,
  primaryRole: roles[0] ?? 'EMPLOYEE',
  permissions: getPermissionsForRoles(roles),
})

describe('M3/M4 路由守卫', () => {
  it('未登录访问受保护页面时保留原目标', () => {
    const target = router.resolve('/courses/4001')

    expect(resolveRouteAccess(target, null)).toEqual({
      name: 'login',
      query: { redirect: '/courses/4001' },
    })
  })

  it('登录过期回跳时提供安全原因标识', () => {
    const target = router.resolve('/courses')

    expect(resolveRouteAccess(target, null, true)).toEqual({
      name: 'login',
      query: { redirect: '/courses', reason: 'session-expired' },
    })
  })

  it('角色不匹配时进入 403 且不携带目标信息', () => {
    const target = router.resolve('/filings/hr')

    expect(resolveRouteAccess(target, makeUser(['EMPLOYEE']))).toEqual({ name: 'forbidden' })
  })

  it('详情页直接从 URL 参数恢复资源编号', () => {
    const target = router.resolve('/certificates/10001')

    expect(resolveRouteAccess(target, makeUser(['HR']))).toBe(true)
    expect(target.name).toBe('certificate-detail')
    expect(target.params.id).toBe('10001')
  })

  it('完整注册 24 个 P0 页面且管理员拥有全部运营权限', () => {
    const p0Routes = router.getRoutes().filter((route) => route.meta.priority === 'P0')
    const adminMenuKeys = getNavigationForRole('ADMIN')
      .flatMap((group) => group.items)
      .map((item) => item.menuKey)

    expect(p0Routes).toHaveLength(24)
    expect(adminMenuKeys).toContain('attendance-management')
    expect(adminMenuKeys).toContain('hr-filing')
    expect(adminMenuKeys).toContain('certificate-management')
    expect(adminMenuKeys).toContain('test-management')
    expect(adminMenuKeys).toContain('employee-management')
    expect(adminMenuKeys).toContain('department-budget-management')
    expect(adminMenuKeys).toContain('trainer-management')
    expect(adminMenuKeys).toContain('rating-management')
  })
})
