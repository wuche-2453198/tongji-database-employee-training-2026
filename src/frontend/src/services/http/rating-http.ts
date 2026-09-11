import { requestApi } from '@/api/client'
import { fromEnvelopeFailure } from '@/api/error'
import { mapCourseRating } from '@/api/mappers/rating'
import type { ApiEnvelopeDto, PageDto, TrainerRatingDto } from '@/api/transport'
import type { RatingService } from '@/domains/rating'

const queryParams = (query: Parameters<RatingService['listMine']>[0]) => ({
  Keyword: query.keyword || undefined,
  StartDateFrom: query.startDateFrom || undefined,
  StartDateTo: query.startDateTo || undefined,
  Page: query.page,
  PageSize: query.pageSize,
})

export const httpRatingService: RatingService = {
  async listMine(query, options) {
    const envelope = await requestApi<ApiEnvelopeDto<PageDto<TrainerRatingDto>>>({
      method: 'GET',
      url: '/api/ratings/my',
      params: queryParams(query),
      signal: options?.signal,
      operation: 'query',
    })
    if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
    return { ...envelope.data, items: envelope.data.items.map(mapCourseRating) }
  },

  async listAll(query, options) {
    const envelope = await requestApi<ApiEnvelopeDto<PageDto<TrainerRatingDto>>>({
      method: 'GET', url: '/api/ratings', params: {
        CourseId: query.courseId ? Number(query.courseId) : undefined,
        TrainerId: query.trainerId ? Number(query.trainerId) : undefined,
        Page: query.page, PageSize: query.pageSize,
      }, signal: options?.signal, operation: 'query',
    })
    if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
    return { ...envelope.data, items: envelope.data.items.map(mapCourseRating) }
  },

  async create(input, options) {
    await requestApi<ApiEnvelopeDto<boolean>>({
      method: 'POST', url: '/api/ratings', signal: options?.signal, operation: 'write',
      data: { CourseId: Number(input.courseId), TrainerId: Number(input.trainerId), Score: input.score,
        RatingComment: input.comment || undefined },
    })
  },

  async verify(id, comment, options) {
    await requestApi<ApiEnvelopeDto<boolean>>({
      method: 'PATCH', url: `/api/ratings/${encodeURIComponent(id)}/verify`, signal: options?.signal, operation: 'write',
      data: { VerifyComment: comment || undefined },
    })
  },
}
