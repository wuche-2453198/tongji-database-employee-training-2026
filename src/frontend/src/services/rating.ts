import type { RatingService } from '@/domains/rating'

export type { RatingService } from '@/domains/rating'

let servicePromise: Promise<RatingService> | undefined

export const getRatingService = (): Promise<RatingService> => {
  servicePromise ??=
    import.meta.env.VITE_USE_MOCK === 'true'
      ? import('@/mocks/rating').then(({ mockRatingService }) => mockRatingService)
      : import('@/services/http/rating-http').then(({ httpRatingService }) => httpRatingService)

  return servicePromise
}
