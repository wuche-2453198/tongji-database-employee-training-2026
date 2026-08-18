import { getPermissionsForRoles } from '@/config/permissions'
import type { AppRole, AppUser, LoginCredentials } from '@/types/auth'
import { AuthServiceError } from '@/types/auth'
import type { AuthService } from '@/services/auth'

const SESSION_KEY = 'training-management.mock-auth-session'
const LEGACY_SESSION_KEY = 'training-management.m3-preview-session'
const MOCK_PASSWORD = 'Demo@123'
const SESSION_TTL_MS = 8 * 60 * 60 * 1000

interface MockAccount {
  password: string
  enabled: boolean
  user: Omit<AppUser, 'permissions'>
}

interface StoredSession {
  account: string
  expiresAt: number
}

const createAccount = (
  account: string,
  displayName: string,
  employeeId: number,
  departmentId: number,
  departmentName: string,
  position: string,
  role: AppRole,
): MockAccount => ({
  password: MOCK_PASSWORD,
  enabled: true,
  user: {
    account,
    displayName,
    employeeId,
    departmentId,
    departmentName,
    position,
    status: 'ACTIVE',
    roles: [role],
    primaryRole: role,
  },
})

const mockAccounts: Record<string, MockAccount> = {
  'employee.demo': createAccount(
    'employee.demo',
    '张三',
    1001,
    101,
    '技术部',
    '前端工程师',
    'EMPLOYEE',
  ),
  'manager.demo': createAccount(
    'manager.demo',
    '李主管',
    1002,
    101,
    '技术部',
    '部门主管',
    'MANAGER',
  ),
  'hr.demo': createAccount('hr.demo', '王 HR', 1003, 102, '人力资源部', '培训专员', 'HR'),
  'admin.demo': createAccount(
    'admin.demo',
    '赵管理员',
    1004,
    103,
    '信息管理部',
    '系统管理员',
    'ADMIN',
  ),
  'disabled.demo': {
    ...createAccount('disabled.demo', '停用账号', 1098, 102, '人力资源部', '培训专员', 'HR'),
    enabled: false,
  },
}

const wait = (duration = 160) =>
  new Promise<void>((resolve) => window.setTimeout(resolve, duration))

const cloneUser = (account: MockAccount): AppUser => ({
  ...account.user,
  roles: [...account.user.roles],
  permissions: getPermissionsForRoles(account.user.roles),
})

const clearSession = () => {
  window.sessionStorage.removeItem(SESSION_KEY)
  window.sessionStorage.removeItem(LEGACY_SESSION_KEY)
}

const readSession = (): StoredSession | null => {
  const raw = window.sessionStorage.getItem(SESSION_KEY)
  if (!raw) return null

  try {
    const session = JSON.parse(raw) as Partial<StoredSession>
    if (typeof session.account !== 'string' || typeof session.expiresAt !== 'number') {
      clearSession()
      return null
    }
    return session as StoredSession
  } catch {
    clearSession()
    return null
  }
}

export const mockAuthService: AuthService = {
  async login(credentials: LoginCredentials) {
    await wait()
    const accountName = credentials.account.trim()

    if (accountName === 'service-error.demo') {
      throw new AuthServiceError('SERVICE_UNAVAILABLE', 'Mock 登录服务暂时不可用。')
    }

    const account = mockAccounts[accountName]
    if (!account || account.password !== credentials.password) {
      throw new AuthServiceError('INVALID_CREDENTIALS', '账号或密码错误。')
    }
    if (!account.enabled) {
      throw new AuthServiceError('ACCOUNT_DISABLED', '当前账号不可用。')
    }

    window.sessionStorage.setItem(
      SESSION_KEY,
      JSON.stringify({ account: accountName, expiresAt: Date.now() + SESSION_TTL_MS }),
    )
    return cloneUser(account)
  },

  async getCurrentUser() {
    await wait(60)
    const session = readSession()
    if (!session) return null
    if (session.expiresAt <= Date.now()) {
      clearSession()
      throw new AuthServiceError('SESSION_EXPIRED', '登录状态已过期。')
    }

    const account = mockAccounts[session.account]
    if (!account?.enabled) {
      clearSession()
      throw new AuthServiceError('SESSION_EXPIRED', '登录状态已失效。')
    }
    return cloneUser(account)
  },

  async logout() {
    await wait(60)
    clearSession()
  },
}
