import type { EmployeeService } from '@/domains/employee'

export type { EmployeeService } from '@/domains/employee'

let servicePromise: Promise<EmployeeService> | undefined

export const getEmployeeService = (): Promise<EmployeeService> => {
  servicePromise ??=
    import.meta.env.VITE_USE_MOCK === 'true'
      ? import('@/mocks/employee').then(({ mockEmployeeService }) => mockEmployeeService)
      : import('@/services/http/employee-http').then(
          ({ httpEmployeeService }) => httpEmployeeService,
        )

  return servicePromise
}
