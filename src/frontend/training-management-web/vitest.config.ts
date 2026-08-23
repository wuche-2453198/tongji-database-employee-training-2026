import { fileURLToPath } from 'node:url'
import { mergeConfig, defineConfig, configDefaults } from 'vitest/config'
import viteConfig from './vite.config.ts'

export default mergeConfig(
  viteConfig,
  defineConfig({
    test: {
      environment: 'jsdom',
      fileParallelism: false,
      include: ['tests/unit/**/*.spec.ts'],
      exclude: [...configDefaults.exclude, 'tests/e2e/**'],
      root: fileURLToPath(new URL('./', import.meta.url)),
      env: {
        VITE_APP_ENV: 'mock',
        VITE_API_BASE_URL: '',
        VITE_USE_MOCK: 'true',
        VITE_REQUEST_TIMEOUT_MS: '10000',
      },
      server: {
        deps: {
          inline: ['element-plus'],
        },
      },
    },
  }),
)
