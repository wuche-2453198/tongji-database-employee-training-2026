import type { RoleCode, CourseStatus } from '@/types/enums'

/** 角色权限映射，与后端 PermissionCodes.cs 一致 */
export const ROLE_PERMISSIONS: Record<RoleCode, string[]> = {
  ADMIN: [
    'auth.me', 'role.read', 'employee.read', 'employee.write',
    'department.read', 'department.write', 'blacklist.read', 'blacklist.write',
    'course.read', 'course.write', 'request.approve', 'request.file',
    'attendance.write', 'rating.verify', 'test.write',
    'certificate.read', 'certificate.write',
  ],
  HR: [
    'auth.me', 'employee.read', 'course.read', 'request.file',
    'attendance.write', 'rating.verify', 'test.write',
    'certificate.read', 'certificate.write',
  ],
  DEPT_MANAGER: [
    'auth.me', 'employee.read', 'course.read', 'request.approve',
  ],
  EMPLOYEE: [
    'auth.me', 'course.read', 'request.create', 'registration.create',
    'rating.create', 'certificate.read',
  ],
}

/** 课程状态对应的页面可操作范围 */
export const COURSE_STATUS_ACTIONS: Record<CourseStatus, string[]> = {
  DRAFT: [],
  PUBLISHED: ['apply', 'detail'],
  CLOSED: ['detail'],
}

/** 导航菜单结构 */
export interface MenuItem {
  title: string
  path: string
  icon?: string
  roles?: RoleCode[]
  children?: MenuItem[]
}

export const MENU_ITEMS: MenuItem[] = [
  {
    title: '工作台',
    path: '/dashboard',
    icon: 'HomeFilled',
  },
  {
    title: '培训中心',
    path: '/training',
    icon: 'Reading',
    children: [
      { title: '课程大厅', path: '/courses', icon: 'Collection' },
      { title: '我的申请', path: '/my-requests', icon: 'Document' },
      { title: '我的报名', path: '/my-registrations', icon: 'List' },
      { title: '我的证书', path: '/my-certificates', icon: 'Medal' },
    ],
  },
  {
    title: '审批管理',
    path: '/approval',
    icon: 'Checked',
    roles: ['DEPT_MANAGER', 'HR', 'ADMIN'],
    children: [
      { title: '主管审批', path: '/dept-approval', icon: 'Select', roles: ['DEPT_MANAGER', 'ADMIN'] },
      { title: 'HR 备案', path: '/hr-filing', icon: 'Stamp', roles: ['HR', 'ADMIN'] },
    ],
  },
  {
    title: '培训运营',
    path: '/operations',
    icon: 'Setting',
    roles: ['HR', 'ADMIN'],
    children: [
      { title: '签到管理', path: '/attendance', icon: 'Calendar' },
      { title: '评分与测试', path: '/evaluations', icon: 'Star' },
    ],
  },
  {
    title: '组织管理',
    path: '/organization',
    icon: 'OfficeBuilding',
    roles: ['ADMIN'],
  },
  {
    title: '课程管理',
    path: '/course-management',
    icon: 'Edit',
    roles: ['ADMIN'],
  },
  {
    title: '风险控制',
    path: '/risk-control',
    icon: 'Warning',
    roles: ['ADMIN'],
  },
  {
    title: '数据分析',
    path: '/statistics',
    icon: 'DataAnalysis',
    roles: ['ADMIN'],
  },
]
