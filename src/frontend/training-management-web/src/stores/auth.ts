import { computed, shallowRef } from 'vue'
import { defineStore } from 'pinia'

import { hasPermission } from '@/config/permissions'
import { getAuthService } from '@/services/auth'
import type { AppPermission, AppUser, AuthErrorCode, LoginCredentials } from '@/types/auth'
import { isAuthServiceError } from '@/types/auth'

type AuthStatus = 'idle' | 'initializing' | 'authenticating' | 'authenticated' | 'anonymous'
type SessionEvent = 'expired' | null

export const useAuthStore = defineStore('auth', () => {
  const currentUser = shallowRef<AppUser | null>(null)
  const status = shallowRef<AuthStatus>('idle')
  const initialized = shallowRef(false)
  const lastErrorCode = shallowRef<AuthErrorCode | null>(null)
  const lastSessionEvent = shallowRef<SessionEvent>(null)

  const isAuthenticated = computed(() => currentUser.value !== null)
  const roles = computed(() => currentUser.value?.roles ?? [])

  const clearLocalState = () => {
    currentUser.value = null
    status.value = 'anonymous'
  }

  const initialize = async (force = false) => {
    if (initialized.value && !force) return currentUser.value

    status.value = 'initializing'
    lastErrorCode.value = null
    try {
      const service = await getAuthService()
      currentUser.value = await service.getCurrentUser()
      status.value = currentUser.value ? 'authenticated' : 'anonymous'
    } catch (error) {
      clearLocalState()
      if (isAuthServiceError(error)) {
        lastErrorCode.value = error.code
        if (error.code === 'SESSION_EXPIRED') lastSessionEvent.value = 'expired'
      } else {
        lastErrorCode.value = 'SERVICE_UNAVAILABLE'
      }
    } finally {
      initialized.value = true
    }

    return currentUser.value
  }

  const login = async (credentials: LoginCredentials) => {
    status.value = 'authenticating'
    lastErrorCode.value = null
    try {
      const service = await getAuthService()
      currentUser.value = await service.login(credentials)
      status.value = 'authenticated'
      initialized.value = true
      lastSessionEvent.value = null
      return currentUser.value
    } catch (error) {
      clearLocalState()
      lastErrorCode.value = isAuthServiceError(error) ? error.code : 'SERVICE_UNAVAILABLE'
      throw error
    }
  }

  const logout = async () => {
    try {
      const service = await getAuthService()
      await service.logout()
    } finally {
      clearLocalState()
      initialized.value = true
      lastErrorCode.value = null
      lastSessionEvent.value = null
    }
  }

  const can = (permission: AppPermission) => hasPermission(currentUser.value, permission)

  return {
    currentUser,
    status,
    initialized,
    lastErrorCode,
    lastSessionEvent,
    isAuthenticated,
    roles,
    initialize,
    login,
    logout,
    can,
  }
})
