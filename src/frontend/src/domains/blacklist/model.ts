import type { DomainPage, DomainPageQuery, ServiceRequestOptions } from '@/domains/shared'

export type BlacklistStatus = 'ACTIVE' | 'RELEASED' | 'UNKNOWN'

export const BLACKLIST_STATUS_LABELS: Record<BlacklistStatus, string> = {
  ACTIVE: '生效中',
  RELEASED: '已解除',
  UNKNOWN: '未知状态',
}

export interface BlacklistQuery extends DomainPageQuery {
  status?: BlacklistStatus
}

export interface BlacklistRecord {
  id: string
  empId: number
  empName: string
  deptName: string
  reason: string
  startDate: string
  endDate: string | null
  status: BlacklistStatus
  statusLabel: string
  operatorEmpId: number | null
  createdAt: string
}

export interface CreateBlacklistInput {
  empId: number
  reason: string
  startDate?: string
  endDate?: string
}

export interface BlacklistService {
  listBlacklists(
    query: BlacklistQuery,
    options?: ServiceRequestOptions,
  ): Promise<DomainPage<BlacklistRecord>>
  createBlacklist(
    input: CreateBlacklistInput,
    options?: ServiceRequestOptions,
  ): Promise<BlacklistRecord>
}
