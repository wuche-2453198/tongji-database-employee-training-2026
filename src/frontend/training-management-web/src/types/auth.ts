export type AppRole = 'EMPLOYEE' | 'MANAGER' | 'HR' | 'ADMIN'

export type AppPermission =
  | 'course:view'
  | 'request:create'
  | 'request:own:view'
  | 'request:department:approve'
  | 'request:hr:file'
  | 'registration:own:view'
  | 'registration:manage'
  | 'attendance:manage'
  | 'certificate:own:view'
  | 'certificate:manage'

export interface AppUser {
  account: string
  displayName: string
  employeeId: number
  departmentId: number | null
  departmentName: string | null
  position: string | null
  status: 'ACTIVE' | 'RESIGNED'
  roles: AppRole[]
  primaryRole: AppRole
  permissions: AppPermission[]
}

export interface LoginCredentials {
  account: string
  password: string
}

export type AuthErrorCode =
  | 'INVALID_CREDENTIALS'
  | 'ACCOUNT_DISABLED'
  | 'SERVICE_UNAVAILABLE'
  | 'SESSION_EXPIRED'
  | 'AUTH_NOT_CONFIGURED'

export class AuthServiceError extends Error {
  constructor(
    public readonly code: AuthErrorCode,
    message: string,
  ) {
    super(message)
    this.name = 'AuthServiceError'
  }
}

export const isAuthServiceError = (value: unknown): value is AuthServiceError =>
  value instanceof AuthServiceError
