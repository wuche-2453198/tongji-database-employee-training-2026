const ACCESS_TOKEN_KEY = 'training-management.access-token'

let unauthorizedHandler: (() => void) | undefined
let unauthorizedPending = false

export const getAccessToken = (): string | null => window.localStorage.getItem(ACCESS_TOKEN_KEY)

export const saveAccessToken = (token: string): void => {
  window.localStorage.setItem(ACCESS_TOKEN_KEY, token)
}

export const clearAccessToken = (): void => {
  window.localStorage.removeItem(ACCESS_TOKEN_KEY)
}

export const setUnauthorizedHandler = (handler: () => void): void => {
  unauthorizedHandler = handler
}

export const notifyUnauthorized = (): void => {
  if (unauthorizedPending) return
  unauthorizedPending = true
  unauthorizedHandler?.()
  window.setTimeout(() => {
    unauthorizedPending = false
  }, 1000)
}
