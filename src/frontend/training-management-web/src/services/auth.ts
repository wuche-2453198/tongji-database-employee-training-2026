import type { AppUser, LoginCredentials } from '@/types/auth'
import { AuthServiceError } from '@/types/auth'

export interface AuthService {
  login(credentials: LoginCredentials): Promise<AppUser>
  getCurrentUser(): Promise<AppUser | null>
  logout(): Promise<void>
}

const unavailableAuthService: AuthService = {
  async login() {
    throw new AuthServiceError('AUTH_NOT_CONFIGURED', '真实认证服务尚未接入。')
  },
  async getCurrentUser() {
    return null
  },
  async logout() {},
}

let servicePromise: Promise<AuthService> | undefined

export const getAuthService = (): Promise<AuthService> => {
  if (import.meta.env.VITE_USE_MOCK !== 'true') return Promise.resolve(unavailableAuthService)

  servicePromise ??= import('@/mocks/auth').then(({ mockAuthService }) => mockAuthService)
  return servicePromise
}
