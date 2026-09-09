/// <reference types="vite/client" />

type AppEnvironment = 'mock' | 'local' | 'integration'

interface ImportMetaEnv {
  readonly VITE_APP_ENV: AppEnvironment
  readonly VITE_API_BASE_URL: string
  readonly VITE_USE_MOCK: string
  readonly VITE_REQUEST_TIMEOUT_MS: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
