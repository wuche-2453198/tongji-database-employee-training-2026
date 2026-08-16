import type { LoginRequest, LoginResponse, AuthUser } from '@/types/auth'

const mockUsers: Record<string, { password: string; user: AuthUser }> = {
  admin: {
    password: 'Password2026!',
    user: {
      empId: 1,
      empName: '系统超级管理员',
      deptName: '公司外训控制中心',
      position: 'IT系统架构师',
      email: 'admin@company.com',
      phone: '13800000001',
      status: 'ACTIVE',
      roles: [{ roleId: 1, roleCode: 'ADMIN', roleName: '系统管理员', permissions: [] }],
      permissions: [],
    },
  },
  employee: {
    password: 'Password2026!',
    user: {
      empId: 2,
      empName: '测试普通员工',
      deptName: '公司外训控制中心',
      position: '软件开发工程师',
      email: 'employee@company.com',
      phone: '13800000002',
      status: 'ACTIVE',
      roles: [{ roleId: 2, roleCode: 'EMPLOYEE', roleName: '普通员工', permissions: [] }],
      permissions: [],
    },
  },
  manager: {
    password: 'Password2026!',
    user: {
      empId: 3,
      empName: '测试部门主管',
      deptName: '公司外训控制中心',
      position: '研发部经理',
      email: 'manager@company.com',
      phone: '13800000003',
      status: 'ACTIVE',
      roles: [{ roleId: 3, roleCode: 'DEPT_MANAGER', roleName: '部门主管', permissions: [] }],
      permissions: [],
    },
  },
  hr: {
    password: 'Password2026!',
    user: {
      empId: 4,
      empName: '测试HR专员',
      deptName: '公司外训控制中心',
      position: '培训发展主管',
      email: 'hr@company.com',
      phone: '13800000004',
      status: 'ACTIVE',
      roles: [{ roleId: 4, roleCode: 'HR', roleName: 'HR管理员', permissions: [] }],
      permissions: [],
    },
  },
}

export function handleMockLogin(data: LoginRequest): LoginResponse {
  const identifier = data.identifier.toLowerCase()
  const userRecord = mockUsers[identifier]

  if (!userRecord || userRecord.password !== data.password) {
    throw { status: 401, message: '用户名或密码错误' }
  }

  const token = 'mock-jwt-' + btoa(JSON.stringify({ sub: userRecord.user.empId, iat: Date.now() }))
  const expiresAt = new Date(Date.now() + 2 * 60 * 60 * 1000).toISOString()

  return {
    accessToken: token,
    tokenType: 'Bearer',
    expiresAt,
    user: userRecord.user,
  }
}

export function handleMockGetMe(token: string): AuthUser {
  if (!token || !token.startsWith('mock-jwt-')) {
    throw { status: 401, message: '未登录或登录已过期' }
  }
  try {
    const payload = JSON.parse(atob(token.replace('mock-jwt-', '')))
    const empId = payload.sub as number
    const user = Object.values(mockUsers).find((u) => u.user.empId === empId)
    if (!user) throw { status: 401, message: '用户不存在' }
    return user.user
  } catch {
    throw { status: 401, message: '未登录或登录已过期' }
  }
}
