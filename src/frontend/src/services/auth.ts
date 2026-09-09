import type { AppUser, LoginCredentials } from '@/types/auth'

export interface AuthService {
  login(credentials: LoginCredentials): Promise<AppUser>
  getCurrentUser(): Promise<AppUser | null>
  logout(): Promise<void>
}

let servicePromise: Promise<AuthService> | undefined

export const getAuthService = (): Promise<AuthService> => {
  servicePromise ??=
    import.meta.env.VITE_USE_MOCK === 'true'
      ? import('@/mocks/auth').then(({ mockAuthService }) => mockAuthService)
      : import('@/services/http/auth-http').then(({ httpAuthService }) => httpAuthService)
  return servicePromise
}
