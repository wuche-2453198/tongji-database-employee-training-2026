import type { AppRole } from '@/types/auth'

export interface DomainPageQuery {
  page: number
  pageSize: number
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
}

export interface DomainPage<T> {
  items: T[]
  page: number
  pageSize: number
  total: number
}

export interface ServiceRequestOptions {
  signal?: AbortSignal
}

export interface ActionEligibility {
  allowed: boolean
  reasonCode?: string
  reason?: string
}

/** StatusTag 支持的语义色，供各领域统一状态展示。 */
export type StatusSemantic = 'success' | 'warning' | 'error' | 'info' | 'neutral'

export interface RoleContext {
  role?: AppRole
  employeeId?: number
}

export const DEFAULT_PAGE_SIZE = 20
export const PAGE_SIZE_OPTIONS = [10, 20, 50] as const
