export type AppEnvironment = 'mock' | 'local' | 'integration' | 'production'

const supportedEnvironments = new Set<AppEnvironment>([
  'mock',
  'local',
  'integration',
  'production',
])

function parseEnvironment(value: string | undefined): AppEnvironment {
  if (value && supportedEnvironments.has(value as AppEnvironment)) {
    return value as AppEnvironment
  }

  throw new Error('VITE_APP_ENV 必须是 mock、local、integration 或 production。')
}

function parseBoolean(name: string, value: string | undefined): boolean {
  if (value === 'true') return true
  if (value === 'false') return false
  throw new Error(`${name} 必须是 true 或 false。`)
}

function parsePositiveInteger(name: string, value: string | undefined): number {
  const parsedValue = Number(value)

  if (!Number.isInteger(parsedValue) || parsedValue <= 0) {
    throw new Error(`${name} 必须是正整数。`)
  }

  return parsedValue
}

const environment = parseEnvironment(import.meta.env.VITE_APP_ENV)
const useMock = parseBoolean('VITE_USE_MOCK', import.meta.env.VITE_USE_MOCK)
const apiBaseUrl = import.meta.env.VITE_API_BASE_URL.trim()

if (environment !== 'mock' && !apiBaseUrl) {
  throw new Error('非 Mock 环境必须配置 VITE_API_BASE_URL。')
}

if (environment !== 'mock' && useMock) {
  throw new Error('非 Mock 环境不得启用 VITE_USE_MOCK。')
}

export const appConfig = Object.freeze({
  environment,
  useMock,
  apiBaseUrl,
  requestTimeoutMs: parsePositiveInteger(
    'VITE_REQUEST_TIMEOUT_MS',
    import.meta.env.VITE_REQUEST_TIMEOUT_MS,
  ),
})
