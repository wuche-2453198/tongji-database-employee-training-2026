import type { BlacklistService } from '@/domains/blacklist'

export type { BlacklistService } from '@/domains/blacklist'

let servicePromise: Promise<BlacklistService> | undefined

export const getBlacklistService = (): Promise<BlacklistService> => {
  servicePromise ??=
    import.meta.env.VITE_USE_MOCK === 'true'
      ? import('@/mocks/blacklist').then(({ mockBlacklistService }) => mockBlacklistService)
      : import('@/services/http/blacklist-http').then(
          ({ httpBlacklistService }) => httpBlacklistService,
        )

  return servicePromise
}
