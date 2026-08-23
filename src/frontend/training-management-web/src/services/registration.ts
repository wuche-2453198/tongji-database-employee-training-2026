import type { RegistrationService } from '@/domains/registration'

export type { RegistrationService } from '@/domains/registration'

let servicePromise: Promise<RegistrationService> | undefined

export const getRegistrationService = (): Promise<RegistrationService> => {
  servicePromise ??=
    import.meta.env.VITE_USE_MOCK === 'true'
      ? import('@/mocks/registration').then(
          ({ mockRegistrationService }) => mockRegistrationService,
        )
      : import('@/services/http/registration-http').then(
          ({ httpRegistrationService }) => httpRegistrationService,
        )

  return servicePromise
}
