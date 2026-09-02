import { domainError } from '@/domains/errors'
import type { RatingService } from '@/domains/rating'

/** TODO(API-Q-021): 讲师评分契约冻结后在此实现 DTO 适配。 */
export const httpRatingService: RatingService = {
  async listMine() {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN')
  },
}
