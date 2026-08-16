import type { DashboardStats } from '@/api/dashboard'
import { mockCourses } from '../data/courses'

/** 从 token 解析 empId */
function getEmpIdFromToken(token?: string): number | null {
  if (!token || !token.startsWith('mock-jwt-')) return null
  try {
    const payload = JSON.parse(atob(token.replace('mock-jwt-', '')))
    return (payload.sub as number) ?? null
  } catch {
    return null
  }
}

/** empId -> role mapping (与 auth mock 一致) */
const EMP_ROLE: Record<number, string> = {
  1: 'ADMIN',
  2: 'EMPLOYEE',
  3: 'DEPT_MANAGER',
  4: 'HR',
}

export function handleMockGetDashboard(_query?: unknown, token?: string): DashboardStats {
  const empId = getEmpIdFromToken(token)
  const role = empId ? (EMP_ROLE[empId] ?? 'EMPLOYEE') : 'EMPLOYEE'

  switch (role) {
    case 'ADMIN':
      return {
        cards: [
          { key: 'published', label: '已发布课程', value: mockCourses.filter((c) => c.courseStatus === 'PUBLISHED').length, icon: 'Collection', color: '#1a56db', path: '/courses' },
          { key: 'employees', label: '在职员工', value: 8, icon: 'User', color: '#16a34a' },
          { key: 'budget', label: '预算使用率', value: 35, icon: 'Money', color: '#ea580c' },
          { key: 'blacklist', label: '黑名单生效', value: 1, icon: 'Warning', color: '#dc2626' },
        ],
        shortcuts: [
          { label: '课程大厅', path: '/courses', icon: 'Collection' },
        ],
      }
    case 'HR':
      return {
        cards: [
          { key: 'tofile', label: '待备案', value: 1, icon: 'Stamp', color: '#ea580c' },
          { key: 'today', label: '今日培训场次', value: 0, icon: 'Calendar', color: '#1a56db' },
          { key: 'totest', label: '待录入成绩', value: 0, icon: 'Edit', color: '#ea580c' },
          { key: 'tocert', label: '待生成证书', value: 0, icon: 'Medal', color: '#16a34a' },
        ],
        shortcuts: [
          { label: '课程大厅', path: '/courses', icon: 'Collection' },
        ],
      }
    case 'DEPT_MANAGER':
      return {
        cards: [
          { key: 'toapprove', label: '待审批申请', value: 2, icon: 'Select', color: '#ea580c' },
          { key: 'monthTrainees', label: '本月培训人数', value: 5, icon: 'User', color: '#1a56db' },
          { key: 'budget', label: '部门预算余额', value: 150000, icon: 'Money', color: '#16a34a' },
          { key: 'upcoming', label: '即将开课', value: 2, icon: 'Clock', color: '#1a56db', path: '/courses' },
        ],
        shortcuts: [
          { label: '课程大厅', path: '/courses', icon: 'Collection' },
        ],
      }
    default:
      return {
        cards: [
          { key: 'pending', label: '待处理申请', value: 1, icon: 'Document', color: '#ea580c', path: '/my-requests' },
          { key: 'upcoming', label: '即将开始课程', value: 1, icon: 'Clock', color: '#1a56db', path: '/courses' },
          { key: 'toreview', label: '待评价', value: 0, icon: 'Star', color: '#ea580c' },
          { key: 'certificates', label: '已获证书', value: 0, icon: 'Medal', color: '#16a34a' },
        ],
        shortcuts: [
          { label: '课程大厅', path: '/courses', icon: 'Collection' },
          { label: '我的申请', path: '/my-requests', icon: 'Document' },
        ],
      }
  }
}
