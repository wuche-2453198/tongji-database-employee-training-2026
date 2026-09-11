import type { DashboardService } from '@/domains/dashboard'

export type { DashboardService } from '@/domains/dashboard'

let servicePromise: Promise<DashboardService> | undefined

export const getDashboardService = (): Promise<DashboardService> => {
  servicePromise ??=
    import.meta.env.VITE_USE_MOCK === 'true'
      ? import('@/mocks/dashboard').then(({ mockDashboardService }) => mockDashboardService)
      : import('@/services/http/dashboard-http').then(
          ({ httpDashboardService }) => httpDashboardService,
        )

  return servicePromise
}
