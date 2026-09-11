import { requestApi } from '@/api/client'
import { mapBlacklist, mapBlacklistPage } from '@/api/mappers/blacklist'
import type { ApiEnvelopeDto, BlacklistDto, PageDto } from '@/api/transport'
import type { BlacklistService } from '@/services/blacklist'

export const httpBlacklistService: BlacklistService = {
  async listBlacklists(query, options) {
    const response = await requestApi<ApiEnvelopeDto<PageDto<BlacklistDto>>>({
      method: 'GET',
      url: '/api/blacklists',
      params: {
        Status: query.status === 'UNKNOWN' ? undefined : query.status || undefined,
        Page: query.page,
        PageSize: query.pageSize,
      },
      signal: options?.signal,
      operation: 'query',
    })
    return mapBlacklistPage(response)
  },

  async createBlacklist(input, options) {
    const response = await requestApi<ApiEnvelopeDto<BlacklistDto>>({
      method: 'POST',
      url: '/api/blacklists',
      data: {
        EmpId: input.empId,
        Reason: input.reason,
        StartDate: input.startDate || undefined,
        EndDate: input.endDate || undefined,
      },
      signal: options?.signal,
      operation: 'write',
    })
    return mapBlacklist(response)
  },

  async updateBlacklist(id, input, options) {
    const response = await requestApi<ApiEnvelopeDto<BlacklistDto>>({
      method: 'PUT', url: `/api/blacklists/${encodeURIComponent(id)}`, signal: options?.signal, operation: 'write',
      data: { Reason: input.reason, EndDate: input.endDate, Status: input.status },
    })
    return mapBlacklist(response)
  },

  async deleteBlacklist(id, options) {
    await requestApi<ApiEnvelopeDto<boolean>>({
      method: 'DELETE', url: `/api/blacklists/${encodeURIComponent(id)}`, signal: options?.signal, operation: 'write',
    })
  },
}
