import { expect, test } from '@playwright/test'

test('未登录访问首页时进入登录页', async ({ page }) => {
  await page.goto('/')

  await expect(page).toHaveURL(/\/login\?redirect=(?:%2F|\/)dashboard$/)
  await expect(page.getByRole('heading', { name: '登录系统' })).toBeVisible()
  await expect(page.getByLabel('员工账号')).toBeVisible()
})

test('登录后回跳详情页并在刷新后恢复', async ({ page }) => {
  await page.goto('/courses/4001')
  await page.getByLabel('员工账号').fill('employee.demo')
  await page.getByLabel('密码').fill('Demo@123')
  await page.getByRole('button', { name: '登录系统' }).click()

  await expect(page).toHaveURL(/\/courses\/4001$/)
  await expect(page.getByRole('heading', { name: '课程详情', exact: true })).toBeVisible()
  await expect(page.getByText('4001', { exact: true })).toBeVisible()

  await page.reload()
  await expect(page.getByRole('heading', { name: '课程详情', exact: true })).toBeVisible()
  await expect(page.getByText('张三')).toBeVisible()
})

test('四角色均可登录、显示正确身份并退出', async ({ page }) => {
  const accounts = [
    {
      account: 'employee.demo',
      name: '张三',
      role: '员工',
      group: '我的培训',
      menu: '我的申请',
      forbiddenMenu: '主管审批',
    },
    {
      account: 'manager.demo',
      name: '李主管',
      role: '部门主管',
      group: '审批管理',
      menu: '主管审批',
      forbiddenMenu: 'HR 备案',
    },
    {
      account: 'hr.demo',
      name: '王 HR',
      role: 'HR',
      group: '审批管理',
      menu: 'HR 备案',
      forbiddenMenu: '主管审批',
    },
    {
      account: 'admin.demo',
      name: '赵管理员',
      role: '管理员',
      group: '培训运营',
      menu: '报名与签到',
      forbiddenMenu: '证书管理',
    },
  ]

  for (const account of accounts) {
    await page.goto('/login')
    await page.getByLabel('员工账号').fill(account.account)
    await page.getByLabel('密码').fill('Demo@123')
    await page.getByRole('button', { name: '登录系统' }).click()
    await expect(page).toHaveURL(/\/dashboard$/)

    const accountButton = page.locator('.app-topbar__account')
    await expect(accountButton).toContainText(account.name)
    await expect(accountButton).toContainText(account.role)

    const sidebar = page.locator('.sidebar-nav')
    await sidebar.getByText(account.group, { exact: true }).click()
    await expect(sidebar.getByText(account.menu, { exact: true })).toBeVisible()
    await expect(sidebar.getByText(account.forbiddenMenu, { exact: true })).toHaveCount(0)

    await accountButton.click()
    await page.getByRole('menuitem', { name: '退出登录' }).click()
    await expect(page).toHaveURL(/\/login$/)
  }
})

test('登录失败、账号停用和服务失败状态可区分', async ({ page }) => {
  await page.goto('/login')
  const accountInput = page.getByLabel('员工账号')
  const passwordInput = page.getByLabel('密码')

  await accountInput.fill('employee.demo')
  await passwordInput.fill('wrong-password')
  await page.getByRole('button', { name: '登录系统' }).click()
  await expect(page.getByText('账号或密码错误，请重新输入')).toBeVisible()

  await accountInput.fill('disabled.demo')
  await passwordInput.fill('Demo@123')
  await page.getByRole('button', { name: '登录系统' }).click()
  await expect(page.getByText('当前账号不可用，请联系管理员')).toBeVisible()

  await accountInput.fill('service-error.demo')
  await passwordInput.fill('Demo@123')
  await page.getByRole('button', { name: '登录系统' }).click()
  await expect(page.getByText('登录服务暂时不可用，请稍后重试')).toBeVisible()
})

test('登录过期时清理会话并回跳原目标', async ({ page }) => {
  await page.addInitScript(() => {
    window.sessionStorage.setItem(
      'training-management.mock-auth-session',
      JSON.stringify({ account: 'employee.demo', expiresAt: Date.now() - 1 }),
    )
  })
  await page.goto('/courses/4001')

  await expect(page).toHaveURL(/\/login\?.*reason=session-expired/)
  await expect(page.getByText('登录状态已过期，请重新登录')).toBeVisible()
})

test('角色越权进入 403 且不泄露目标信息', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('员工账号').fill('employee.demo')
  await page.getByLabel('密码').fill('Demo@123')
  await page.getByRole('button', { name: '登录系统' }).click()
  await expect(page).toHaveURL(/\/dashboard$/)
  await page.goto('/filings/hr')

  await expect(page).toHaveURL(/\/403$/)
  await expect(page.getByText('当前角色无权访问此页面')).toBeVisible()
  await expect(page.getByText('/filings/hr')).toHaveCount(0)
})

test('未知地址使用无账号上下文的 404 页面', async ({ page }) => {
  await page.goto('/not-a-real-page')

  await expect(page.getByText('404', { exact: true })).toBeVisible()
  await expect(page.getByRole('heading', { name: '页面不存在' })).toBeVisible()
  await expect(page.getByText('当前账号')).toHaveCount(0)
})

test('1024px 主布局尺寸与管理员菜单边界正确', async ({ page }) => {
  await page.setViewportSize({ width: 1024, height: 768 })
  await page.goto('/login')
  await page.getByLabel('员工账号').fill('admin.demo')
  await page.getByLabel('密码').fill('Demo@123')
  await page.getByRole('button', { name: '登录系统' }).click()
  await expect(page).toHaveURL(/\/dashboard$/)

  const topbar = page.locator('.app-topbar')
  const sidebar = page.locator('.sidebar-nav')
  await expect(topbar).toHaveCSS('height', '56px')
  await expect(sidebar).toHaveCSS('width', '224px')
  await sidebar.getByRole('menuitem', { name: '培训运营' }).click()
  await expect(sidebar.getByText('报名与签到', { exact: true })).toBeVisible()
  await expect(sidebar.getByText('HR 备案', { exact: true })).toHaveCount(0)
  await expect(sidebar.getByText('证书管理', { exact: true })).toHaveCount(0)

  await page.getByRole('button', { name: '收起侧边导航' }).click()
  await expect(sidebar).toHaveCSS('width', '64px')
  await page.screenshot({ path: 'test-results/m4-dashboard-1024.png', fullPage: true })
})

test('打开 M2 公共组件展示并使用确认弹窗', async ({ page }) => {
  const clientProblems: string[] = []
  page.on('console', (message) => {
    if (message.type() === 'warning' || message.type() === 'error') {
      clientProblems.push(message.text())
    }
  })
  page.on('pageerror', (error) => clientProblems.push(error.message))

  await page.goto('/components')

  await expect(page.getByRole('heading', { name: '公共组件展示' })).toBeVisible()
  await expect(page.getByRole('status', { name: '状态：待审批' })).toBeVisible()

  await page.getByRole('button', { name: '打开确认弹窗' }).click()
  await expect(page.getByRole('dialog')).toBeVisible()
  await expect(page.getByRole('heading', { name: '完成 HR 备案' })).toBeVisible()
  await page.getByRole('button', { name: '取消' }).click()
  await expect(page.getByRole('dialog')).toBeHidden()
  expect(clientProblems).toEqual([])
})

test('M5 API 边界覆盖列表、空态、403、409与结果未知', async ({ page }) => {
  await page.setViewportSize({ width: 1024, height: 768 })
  await page.goto('/api-boundary?scenario=normal')
  await expect(page.getByRole('heading', { name: 'API 与 Mock 边界验收' })).toBeVisible()
  await expect(page.getByText('数据库性能优化实战')).toBeVisible()
  const scenarioPicker = page.locator('.api-boundary__controls .el-select__wrapper')

  await scenarioPicker.click()
  await page.getByRole('option', { name: '空结果' }).click()
  await expect(page.getByRole('heading', { name: '暂无课程' })).toBeVisible()

  await scenarioPicker.click()
  await page.getByRole('option', { name: '无权访问（403）' }).click()
  await expect(page.getByText('当前账号无权查看该课程范围。').first()).toBeVisible()

  await scenarioPicker.click()
  await page.getByRole('option', { name: '报名冲突（409）' }).click()
  await page.getByRole('button', { name: '模拟报名' }).click()
  await expect(page.getByText('课程名额已满，请刷新课程状态。').first()).toBeVisible()

  await scenarioPicker.click()
  await page.getByRole('option', { name: '写操作结果未知' }).click()
  await page.getByRole('button', { name: '模拟报名' }).click()
  await expect(page.getByTestId('ui-error')).toContainText('操作结果未知')
  await expect(page.getByText('请查询最终状态')).toBeVisible()
  await expect(page.getByRole('button', { name: '重新加载' })).toHaveCount(0)
  await page.screenshot({ path: 'test-results/m5-api-boundary-1024.png', fullPage: true })
})

test('M6 部门主管入口明确区分基础就绪与业务未实现', async ({ page }) => {
  const clientProblems: string[] = []
  page.on('console', (message) => {
    if (message.type() === 'warning' || message.type() === 'error') {
      clientProblems.push(message.text())
    }
  })
  page.on('pageerror', (error) => clientProblems.push(error.message))

  await page.setViewportSize({ width: 1440, height: 900 })
  await page.goto('/login')
  await page.getByLabel('员工账号').fill('manager.demo')
  await page.getByLabel('密码').fill('Demo@123')
  await page.getByRole('button', { name: '登录系统' }).click()
  await expect(page).toHaveURL(/\/dashboard$/)

  await page.goto('/approvals/department')
  await expect(page.getByRole('heading', { name: '主管审批', exact: true })).toBeVisible()
  await expect(page.getByText('主管审批尚未实现业务功能')).toBeVisible()
  await expect(page.getByText('/approvals/department', { exact: true })).toBeVisible()
  await page.screenshot({ path: 'test-results/m6-manager-approval-1440.png', fullPage: true })

  expect(clientProblems).toEqual([])
})
