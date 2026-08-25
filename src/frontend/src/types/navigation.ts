import type { AppRole } from './auth'

export type { AppRole, AppUser } from './auth'

export interface NavigationItem {
  label: string
  path: string
  menuKey: string
  roles: AppRole[]
}

export interface NavigationGroup {
  label: string
  key: string
  items: NavigationItem[]
}
