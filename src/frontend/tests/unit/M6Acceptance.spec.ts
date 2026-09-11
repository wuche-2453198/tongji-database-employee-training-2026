import { describe, expect, it } from 'vitest'

import { getNavigationForRole } from '@/config/navigation'
import { resolveScrollPosition } from '@/router'
import type { AppRole } from '@/types/auth'
import {
  compactListQuery,
  readPositiveQueryInteger,
  readQueryString,
} from '@/utils/list-route-state'

const expectedMenuKeys: Record<AppRole, string[]> = {
  EMPLOYEE: [
    'dashboard',
    'courses',
    'my-requests',
    'my-registrations',
    'my-certificates',
    'my-ratings',
  ],
  DEPT_MANAGER: ['dashboard', 'courses', 'department-approval', 'blacklist-management'],
  HR: [
    'dashboard',
    'courses',
    'hr-filing',
    'department-budget-management',
    'blacklist-management',
    'trainer-management',
    'rating-management',
    'attendance-management',
    'certificate-management',
    'test-management',
  ],
  ADMIN: [
    'dashboard',
    'courses',
    'department-approval',
    'hr-filing',
    'employee-management',
    'department-budget-management',
    'blacklist-management',
    'trainer-management',
    'rating-management',
    'attendance-management',
    'certificate-management',
    'test-management',
  ],
}

describe('M6 公共基础能力验收', () => {
  it.each(Object.entries(expectedMenuKeys) as Array<[AppRole, string[]]>)(
    '%s 角色只获得已声明菜单',
    (role, expected) => {
      const actual = getNavigationForRole(role)
        .flatMap((group) => group.items)
        .map((item) => item.menuKey)

      expect(actual).toEqual(expected)
      expect(new Set(actual).size).toBe(actual.length)
    },
  )

  it('列表查询参数提供安全默认值并移除空筛选', () => {
    expect(readQueryString(['  数据库  ', '忽略值'])).toBe('数据库')
    expect(readPositiveQueryInteger('2', 1)).toBe(2)
    expect(readPositiveQueryInteger('-1', 1)).toBe(1)
    expect(compactListQuery({ keyword: '', status: 'PUBLISHED', page: 2, end: null })).toEqual({
      status: 'PUBLISHED',
      page: '2',
    })
  })

  it('浏览器前进后退优先恢复保存的滚动位置', () => {
    const savedPosition = { left: 0, top: 480 }
    expect(resolveScrollPosition({} as never, {} as never, savedPosition)).toEqual(savedPosition)
    expect(resolveScrollPosition({} as never, {} as never, null)).toEqual({ top: 0 })
  })
})
