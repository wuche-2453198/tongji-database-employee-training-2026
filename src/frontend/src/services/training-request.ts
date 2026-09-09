import type { TrainingRequestService } from '@/domains/training-request'

export type { TrainingRequestService } from '@/domains/training-request'

let servicePromise: Promise<TrainingRequestService> | undefined

export const getTrainingRequestService = (): Promise<TrainingRequestService> => {
  servicePromise ??=
    import.meta.env.VITE_USE_MOCK === 'true'
      ? import('@/mocks/training-request').then(
          ({ mockTrainingRequestService }) => mockTrainingRequestService,
        )
      : import('@/services/http/training-request-http').then(
          ({ httpTrainingRequestService }) => httpTrainingRequestService,
        )

  return servicePromise
}
