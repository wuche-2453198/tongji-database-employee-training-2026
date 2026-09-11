import { requestApi } from '@/api/client'
import { mapTrainer, mapTrainerPage } from '@/api/mappers/trainer'
import type { ApiEnvelopeDto, PageDto, TrainerDto } from '@/api/transport'
import type { TrainerInput, TrainerService } from '@/domains/trainer'

const toPayload = (input: TrainerInput) => ({ TrainerName: input.name, Title: input.title || undefined,
  Company: input.company || undefined, Phone: input.phone || undefined, Email: input.email || undefined,
  StarLevel: input.starLevel, IsInternal: input.isInternal ? 'Y' : 'N' })
export const httpTrainerService: TrainerService = {
  async list(query, options) {
    const response = await requestApi<ApiEnvelopeDto<PageDto<TrainerDto>>>({ method: 'GET', url: '/api/trainers', signal: options?.signal, operation: 'query', params: { TrainerName: query.name || undefined, Company: query.company || undefined, IsInternal: query.internal, Page: query.page, PageSize: query.pageSize } })
    return mapTrainerPage(response)
  },
  async create(input, options) { const response = await requestApi<ApiEnvelopeDto<TrainerDto>>({ method: 'POST', url: '/api/trainers', signal: options?.signal, operation: 'write', data: toPayload(input) }); return mapTrainer(response) },
  async update(id, input, options) { const response = await requestApi<ApiEnvelopeDto<TrainerDto>>({ method: 'PUT', url: `/api/trainers/${encodeURIComponent(id)}`, signal: options?.signal, operation: 'write', data: toPayload(input) }); return mapTrainer(response) },
}
