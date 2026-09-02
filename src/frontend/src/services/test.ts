import type { TestService } from '@/domains/test'

export type { TestService } from '@/domains/test'

let servicePromise: Promise<TestService> | undefined

export const getTestService = (): Promise<TestService> => {
  servicePromise ??=
    import.meta.env.VITE_USE_MOCK === 'true'
      ? import('@/mocks/test').then(({ mockTestService }) => mockTestService)
      : import('@/services/http/test-http').then(({ httpTestService }) => httpTestService)

  return servicePromise
}
