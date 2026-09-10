import { requestApi } from '@/api/client'
import { fromEnvelopeFailure } from '@/api/error'
import type { ApiEnvelopeDto } from '@/api/transport'
import type { DashboardService, DashboardStats } from '@/domains/dashboard'

export const httpDashboardService: DashboardService = {
  async getStats(options) {
    const envelope = await requestApi<ApiEnvelopeDto<DashboardStats>>({
      method: 'GET',
      url: '/api/dashboard/stats',
      signal: options?.signal,
      operation: 'query',
    })
    if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
    return envelope.data
  },
}
