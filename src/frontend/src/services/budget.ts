import type { DepartmentBudgetService } from '@/domains/budget'
export type { DepartmentBudgetService } from '@/domains/budget'

let servicePromise: Promise<DepartmentBudgetService> | undefined
export const getDepartmentBudgetService = (): Promise<DepartmentBudgetService> => {
  servicePromise ??= import.meta.env.VITE_USE_MOCK === 'true'
    ? import('@/mocks/budget').then(({ mockDepartmentBudgetService }) => mockDepartmentBudgetService)
    : import('@/services/http/budget-http').then(({ httpDepartmentBudgetService }) => httpDepartmentBudgetService)
  return servicePromise
}
