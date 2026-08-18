import type { AppPermission, AppRole, AppUser } from '@/types/auth'

const rolePermissions: Record<AppRole, readonly AppPermission[]> = {
  EMPLOYEE: [
    'course:view',
    'request:create',
    'request:own:view',
    'registration:own:view',
    'certificate:own:view',
  ],
  MANAGER: ['course:view', 'request:department:approve'],
  HR: [
    'course:view',
    'request:hr:file',
    'registration:manage',
    'attendance:manage',
    'certificate:manage',
  ],
  ADMIN: ['course:view', 'registration:manage', 'attendance:manage'],
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
