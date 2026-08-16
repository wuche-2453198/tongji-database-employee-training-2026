import type { RoleCode } from './enums'

/** 登录请求 */
export interface LoginRequest {
  identifier: string
  password: string
}

/** 登录响应 */
export interface LoginResponse {
  accessToken: string
  tokenType: string
  expiresAt: string
  user: AuthUser
}

/** 角色信息 */
export interface AuthRole {
  roleId: number
  roleCode: string
  roleName: string
  permissions: string[]
}

/** 当前用户信息 */
export interface AuthUser {
  empId: number
  empName: string
  deptName: string | null
  position: string | null
  email: string | null
  phone: string | null
  status: string
  roles: AuthRole[]
  permissions: string[]
}

/** 认证 Store 状态 */
export interface AuthState {
  token: string | null
  user: AuthUser | null
  isAuthenticated: boolean
  roles: RoleCode[]
}
