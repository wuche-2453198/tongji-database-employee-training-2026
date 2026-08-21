import { apiRequest } from './client'
import { useMock } from '@/config/mock'
import type { LoginRequest, LoginResponse, AuthUser } from '@/types/auth'

/** 登录 */
export function loginApi(data: LoginRequest) {
  return apiRequest<LoginResponse>('POST', '/api/auth/login', data, {
    mock: useMock('auth'),
  })
}

/** 获取当前用户 */
export function getCurrentUserApi() {
  return apiRequest<AuthUser>('GET', '/api/auth/me', undefined, {
    mock: useMock('auth'),
  })
}
