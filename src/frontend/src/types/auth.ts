export type AppRole = 'EMPLOYEE' | 'DEPT_MANAGER' | 'HR' | 'ADMIN'

export type AppPermission =
  | 'auth.me'
  | 'role.read'
  | 'employee.read'
  | 'employee.write'
  | 'department.read'
  | 'department.write'
  | 'blacklist.read'
  | 'blacklist.write'
  | 'course.read'
  | 'course.write'
  | 'request.create'
  | 'request.approve'
  | 'request.file'
  | 'registration.create'
  | 'attendance.write'
  | 'rating.create'
  | 'rating.verify'
  | 'test.write'
  | 'certificate.read'
  | 'certificate.write'

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
