/**
 * 按模块的 Mock 开关。
 * 默认跟随全局 VITE_USE_MOCK；当某模块后端接口尚未就绪时，
 * 可通过独立环境变量强制该模块继续走 Mock，而不影响其它模块接真实接口。
 *
 * 例如：
 *   VITE_USE_MOCK=false              # 全局接真实接口
 *   VITE_USE_MOCK_TRAINING_REQUEST=true  # 但申请模块后端未修复，仍走 Mock
 */

export type MockModule = 'auth' | 'course' | 'trainingRequest' | 'dashboard'

const GLOBAL_USE_MOCK = import.meta.env.VITE_USE_MOCK === 'true'

function readBool(envKey: string): boolean | undefined {
  const raw = import.meta.env[envKey]
  if (raw === undefined || raw === '') return undefined
  return raw === 'true' || raw === '1'
}

const OVERRIDES: Record<MockModule, boolean | undefined> = {
  auth: readBool('VITE_USE_MOCK_AUTH'),
  course: readBool('VITE_USE_MOCK_COURSE'),
  trainingRequest: readBool('VITE_USE_MOCK_TRAINING_REQUEST'),
  dashboard: readBool('VITE_USE_MOCK_DASHBOARD'),
}

/** 某模块当前是否走 Mock */
export function useMock(module: MockModule): boolean {
  return OVERRIDES[module] ?? GLOBAL_USE_MOCK
}

export { GLOBAL_USE_MOCK }
