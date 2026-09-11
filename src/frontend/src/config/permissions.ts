import type { AppPermission, AppRole, AppUser } from '@/types/auth'

const rolePermissions: Record<AppRole, readonly AppPermission[]> = {
  EMPLOYEE: [
    'auth.me',
    'course.read',
    'request.create',
    'registration.create',
    'rating.create',
    'certificate.read',
  ],
  DEPT_MANAGER: [
    'auth.me',
    'employee.read',
    'course.read',
    'request.approve',
    'blacklist.read',
    'blacklist.write',
  ],
  HR: [
    'auth.me',
    'employee.read',
    'course.read',
    'request.file',
    'attendance.write',
    'rating.verify',
    'test.write',
    'certificate.read',
    'certificate.write',
  ],
  ADMIN: [
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
    'request.approve',
    'request.file',
    'attendance.write',
    'rating.verify',
    'test.write',
    'certificate.read',
    'certificate.write',
  ],
}

export const getPermissionsForRoles = (roles: readonly AppRole[]): AppPermission[] => [
  ...new Set(roles.flatMap((role) => rolePermissions[role])),
]

export const hasRole = (user: AppUser | null, role: AppRole) => Boolean(user?.roles.includes(role))

export const hasAnyRole = (user: AppUser | null, roles: readonly AppRole[]) =>
  Boolean(user && roles.some((role) => user.roles.includes(role)))

export const hasPermission = (user: AppUser | null, permission: AppPermission) =>
  Boolean(user?.permissions.includes(permission))

export const hasAnyPermission = (user: AppUser | null, permissions: readonly AppPermission[]) =>
  Boolean(user && permissions.some((permission) => user.permissions.includes(permission)))
