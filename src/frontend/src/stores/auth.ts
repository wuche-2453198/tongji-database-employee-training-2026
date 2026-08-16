import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { AuthUser, LoginRequest } from '@/types/auth'
import type { RoleCode } from '@/types/enums'
import { loginApi, getCurrentUserApi } from '@/api/auth'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('access_token'))
  const user = ref<AuthUser | null>(null)

  /** 是否已尝试过恢复会话（路由守卫依赖此值，避免重复调用 /api/auth/me） */
  const initialized = ref(false)

  const isAuthenticated = computed(() => !!token.value && !!user.value)

  const roles = computed<RoleCode[]>(() => {
    if (!user.value) return []
    return user.value.roles.map((r) => r.roleCode as RoleCode)
  })

  const permissions = computed<string[]>(() => {
    if (!user.value) return []
    return user.value.permissions
  })

  /** 检查是否拥有指定角色 */
  function hasRole(roleList: RoleCode[]): boolean {
    if (roleList.length === 0) return true
    return roles.value.some((r) => roleList.includes(r))
  }

  /** 检查是否拥有指定权限 */
  function hasPermission(code: string): boolean {
    return permissions.value.includes(code)
  }

  /** 登录 */
  async function login(data: LoginRequest): Promise<{ success: boolean; message: string }> {
    const res = await loginApi(data)
    if (!res.success || !res.data) {
      return { success: false, message: res.message || '登录失败' }
    }
    token.value = res.data.accessToken
    user.value = res.data.user
    localStorage.setItem('access_token', res.data.accessToken)
    localStorage.setItem('user_info', JSON.stringify(res.data.user))
    return { success: true, message: 'ok' }
  }

  /** 从本地 Token 恢复登录状态（幂等，多次调用只发起一次 /api/auth/me） */
  let restorePromise: Promise<boolean> | null = null

  function restoreSession(): Promise<boolean> {
    if (!restorePromise) {
      restorePromise = doRestoreSession().finally(() => {
        initialized.value = true
      })
    }
    return restorePromise
  }

  async function doRestoreSession(): Promise<boolean> {
    const savedToken = localStorage.getItem('access_token')
    if (!savedToken) return false
    token.value = savedToken

    const res = await getCurrentUserApi()
    if (!res.success || !res.data) {
      logout()
      return false
    }
    user.value = res.data
    return true
  }

  /** 退出登录 */
  function logout(): void {
    token.value = null
    user.value = null
    localStorage.removeItem('access_token')
    localStorage.removeItem('user_info')
  }

  return { token, user, initialized, isAuthenticated, roles, permissions, hasRole, hasPermission, login, restoreSession, logout }
})
