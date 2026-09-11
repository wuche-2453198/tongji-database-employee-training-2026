import type { TrainerService } from '@/domains/trainer'
export type { TrainerService } from '@/domains/trainer'
let servicePromise: Promise<TrainerService> | undefined
export const getTrainerService = (): Promise<TrainerService> => {
  servicePromise ??=
    import.meta.env.VITE_USE_MOCK === 'true'
      ? import('@/mocks/trainer').then(({ mockTrainerService }) => mockTrainerService)
      : import('@/services/http/trainer-http').then(({ httpTrainerService }) => httpTrainerService)
  return servicePromise
}
