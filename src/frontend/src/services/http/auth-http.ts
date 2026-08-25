import { fromEnvelopeFailure } from '@/api/error'
import { clearAccessToken, getAccessToken, saveAccessToken } from '@/api/auth-session'
import { requestApi } from '@/api/client'
import type { ApiEnvelopeDto } from '@/api/transport'
import type { AuthService } from '@/services/auth'
import type { AppPermission, AppRole, AppUser } from '@/types/auth'
import { AuthServiceError } from '@/types/auth'
import { isServiceError } from '@/types/api'

interface AuthRoleDto {
  roleId: number
  roleCode: string
  roleName: string
  permissions: string[]
}

interface AuthUserDto {
  empId: number
  empName: string
  deptName: string | null
  position: string | null
  email: string | null
  phone: string | null
  status: string
  roles: AuthRoleDto[]
  permissions: string[]
}

interface LoginResponseDto {
  accessToken: string
  tokenType: string
  expiresAt: string
  user: AuthUserDto
}

const knownRoles = new Set<AppRole>(['ADMIN', 'HR', 'DEPT_MANAGER', 'EMPLOYEE'])
const permissionCodes = new Set<AppPermission>([
  'auth.me',
  'role.read',
  'employee.read',
  'employee.write',
  'department.read',
  'department.write',
  'blacklist.read',
  'blacklist.write',
  'course.read',
  'course.write',
  'request.create',
  'request.approve',
  'request.file',
  'registration.create',
  'attendance.write',
  'rating.create',
  'rating.verify',
  'test.write',
  'certificate.read',
  'certificate.write',
])

const normalizeRole = (value: string): AppRole | null => {
  const normalized = value.trim().toUpperCase().replace(/ /g, '_')
  const compatible = normalized === 'MANAGER' ? 'DEPT_MANAGER' : normalized
  return knownRoles.has(compatible as AppRole) ? (compatible as AppRole) : null
}

const mapUser = (dto: AuthUserDto, identifier?: string): AppUser => {
  const roles = dto.roles
    .map((role) => normalizeRole(role.roleCode))
    .filter((role) => role !== null)
  const normalizedRoles = roles.length > 0 ? roles : (['EMPLOYEE'] satisfies AppRole[])
  const priority: AppRole[] = ['ADMIN', 'HR', 'DEPT_MANAGER', 'EMPLOYEE']
  const primaryRole = priority.find((role) => normalizedRoles.includes(role)) ?? 'EMPLOYEE'

  return {
    account: identifier || String(dto.empId),
    displayName: dto.empName,
    employeeId: dto.empId,
    departmentId: null,
    departmentName: dto.deptName,
    position: dto.position,
    status: dto.status === 'ACTIVE' ? 'ACTIVE' : 'RESIGNED',
    roles: normalizedRoles,
    primaryRole,
    permissions: dto.permissions.filter((permission): permission is AppPermission =>
      permissionCodes.has(permission as AppPermission),
    ),
  }
}

const unwrap = <T>(envelope: ApiEnvelopeDto<T>): T => {
  if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
  return envelope.data
}

const asAuthError = (error: unknown, operation: 'login' | 'session'): AuthServiceError => {
  if (isServiceError(error)) {
    if (error.ui.kind === 'unauthorized') {
      return new AuthServiceError(
        operation === 'login' ? 'INVALID_CREDENTIALS' : 'SESSION_EXPIRED',
        operation === 'login' ? '账号或密码错误。' : '登录状态已过期。',
      )
    }
    return new AuthServiceError('SERVICE_UNAVAILABLE', error.ui.message)
  }
  return new AuthServiceError('SERVICE_UNAVAILABLE', '认证服务暂时不可用。')
}

export const httpAuthService: AuthService = {
  async login(credentials) {
    clearAccessToken()
    try {
      const envelope = await requestApi<ApiEnvelopeDto<LoginResponseDto>>({
        method: 'POST',
        url: '/api/auth/login',
        data: { identifier: credentials.account.trim(), password: credentials.password },
        operation: 'write',
      })
      const response = unwrap(envelope)
      if (response.tokenType.toLowerCase() !== 'bearer' || !response.accessToken) {
        throw new AuthServiceError('SERVICE_UNAVAILABLE', '认证服务返回了无效的访问令牌。')
      }
      saveAccessToken(response.accessToken)
      return mapUser(response.user, credentials.account.trim())
    } catch (error) {
      clearAccessToken()
      if (error instanceof AuthServiceError) throw error
      throw asAuthError(error, 'login')
    }
  },

  async getCurrentUser() {
    if (!getAccessToken()) return null
    try {
      const envelope = await requestApi<ApiEnvelopeDto<AuthUserDto>>({
        method: 'GET',
        url: '/api/auth/me',
        operation: 'query',
      })
      return mapUser(unwrap(envelope))
    } catch (error) {
      clearAccessToken()
      throw asAuthError(error, 'session')
    }
  },

  async logout() {
    clearAccessToken()
  },
}
