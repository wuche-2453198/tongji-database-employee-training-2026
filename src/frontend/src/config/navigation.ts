import type { AppRole, NavigationGroup } from '@/types/navigation'

const allRoles: AppRole[] = ['EMPLOYEE', 'DEPT_MANAGER', 'HR', 'ADMIN']

export const navigationGroups: NavigationGroup[] = [
  {
    label: '工作台',
    key: 'workspace',
    items: [
      {
        label: '首页 / 控制台',
        path: '/dashboard',
        menuKey: 'dashboard',
        roles: allRoles,
      },
      {
        label: '课程中心',
        path: '/courses',
        menuKey: 'courses',
        roles: allRoles,
      },
    ],
  },
  {
    label: '我的培训',
    key: 'my-training',
    items: [
      {
        label: '我的申请',
        path: '/my/requests',
        menuKey: 'my-requests',
        roles: ['EMPLOYEE'],
      },
      {
        label: '我的报名',
        path: '/my/registrations',
        menuKey: 'my-registrations',
        roles: ['EMPLOYEE'],
      },
      {
        label: '我的证书',
        path: '/my/certificates',
        menuKey: 'my-certificates',
        roles: ['EMPLOYEE'],
      },
      {
        label: '我的评分',
        path: '/my/ratings',
        menuKey: 'my-ratings',
        roles: ['EMPLOYEE'],
      },
      {
        label: '我的成绩',
        path: '/my/tests',
        menuKey: 'my-test-scores',
        roles: ['EMPLOYEE'],
      },
    ],
  },
  {
    label: '审批管理',
    key: 'approval-management',
    items: [
      {
        label: '主管审批',
        path: '/approvals/department',
        menuKey: 'department-approval',
        roles: ['DEPT_MANAGER', 'ADMIN'],
      },
      {
        label: 'HR 备案',
        path: '/filings/hr',
        menuKey: 'hr-filing',
        roles: ['HR', 'ADMIN'],
      },
    ],
  },
  {
    label: '组织管理',
    key: 'organization-management',
    items: [
      {
        label: '员工管理',
        path: '/organization/employees',
        menuKey: 'employee-management',
        roles: ['ADMIN'],
      },
      {
        label: '部门预算管理',
        path: '/organization/budgets',
        menuKey: 'department-budget-management',
        roles: ['HR', 'ADMIN'],
      },
      {
        label: '黑名单管理',
        path: '/organization/blacklist',
        menuKey: 'blacklist-management',
        roles: ['DEPT_MANAGER', 'HR', 'ADMIN'],
      },
    ],
  },
  {
    label: '培训运营',
    key: 'training-operations',
    items: [
      {
        label: '讲师管理',
        path: '/operations/trainers',
        menuKey: 'trainer-management',
        roles: ['HR', 'ADMIN'],
      },
      {
        label: '讲师评分管理',
        path: '/operations/ratings',
        menuKey: 'rating-management',
        roles: ['HR', 'ADMIN'],
      },
      {
        label: '报名与签到',
        path: '/operations/attendance',
        menuKey: 'attendance-management',
        roles: ['HR', 'ADMIN'],
      },
      {
        label: '证书管理',
        path: '/operations/certificates',
        menuKey: 'certificate-management',
        roles: ['HR', 'ADMIN'],
      },
      {
        label: '测试成绩',
        path: '/operations/tests',
        menuKey: 'test-management',
        roles: ['HR', 'ADMIN'],
      },
    ],
  },
]

export const getNavigationForRole = (role: AppRole) => getNavigationForRoles([role])

export const getNavigationForRoles = (roles: readonly AppRole[]) =>
  navigationGroups
    .map((group) => ({
      ...group,
      items: group.items.filter((item) => item.roles.some((role) => roles.includes(role))),
    }))
    .filter((group) => group.items.length > 0)

const detailMenuKeys: Record<string, Partial<Record<AppRole, string>>> = {
  'request-detail': {
    EMPLOYEE: 'my-requests',
    DEPT_MANAGER: 'department-approval',
    HR: 'hr-filing',
    ADMIN: 'department-approval',
  },
  'registration-detail': {
    EMPLOYEE: 'my-registrations',
    HR: 'attendance-management',
    ADMIN: 'attendance-management',
  },
  'certificate-detail': {
    EMPLOYEE: 'my-certificates',
    HR: 'certificate-management',
    ADMIN: 'certificate-management',
  },
}

export const getActiveMenuKey = (routeName: unknown, roles: readonly AppRole[], fallback = '') => {
  if (typeof routeName !== 'string') return fallback
  const mapping = detailMenuKeys[routeName]
  return roles.map((role) => mapping?.[role]).find(Boolean) ?? fallback
}
