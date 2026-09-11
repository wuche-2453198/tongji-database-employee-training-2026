import { expect, test, type Page } from '@playwright/test'

async function login(page: Page, account: string) {
  await page.goto('/login')
  await page.getByLabel('员工账号').fill(account)
  await page.getByLabel('密码').fill('Demo@123')
  await page.getByRole('button', { name: '登录系统' }).click()
  await expect(page).toHaveURL(/\/dashboard$/)
}

async function gotoSnapshot(page: Page, path: string, snapshot: string) {
  await page.goto(`${path}?mockSnapshot=${snapshot}`)
  await page.evaluate((value) => {
    ;(window as unknown as { __trainingMockLoad?: (id: string) => void }).__trainingMockLoad?.(
      value,
    )
  }, snapshot)
  await page.reload()
}

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() => {
    window.sessionStorage.removeItem('training-management.mock-business-state')
  })
})

test('HR 查看按员工+课程聚合的 PRE/POST 成绩与提升率', async ({ page }) => {
  await login(page, 'hr.demo')
  await gotoSnapshot(page, '/operations/tests', 'FLOW-SNAPSHOT-05')
  await expect(page.getByRole('heading', { name: '测试成绩', exact: true })).toBeVisible()

  await expect(page.getByRole('cell', { name: '张三', exact: true })).toBeVisible()
  await expect(page.getByRole('cell', { name: '数据库性能优化实战', exact: true })).toBeVisible()
  await expect(page.getByLabel('状态：78 分')).toBeVisible()
  await expect(page.getByLabel('状态：85 分')).toBeVisible()
  await expect(page.getByRole('cell', { name: '+7', exact: true })).toBeVisible()
  await expect(page.getByRole('cell', { name: '+9%', exact: true })).toBeVisible()
})

test('HR 重复录入同一类型成绩得到明确冲突提示', async ({ page }) => {
  await login(page, 'hr.demo')
  await gotoSnapshot(page, '/operations/tests', 'FLOW-SNAPSHOT-05')

  await page.getByRole('button', { name: '录入成绩' }).click()
  await page.getByRole('combobox', { name: '员工' }).click()
  await page.getByRole('option', { name: /张三/ }).click()
  await page.getByRole('combobox', { name: '课程' }).click()
  await page.getByRole('option', { name: /数据库性能优化实战/ }).click()
  await page.getByRole('button', { name: '提交' }).click()

  const dialog = page.getByRole('dialog')
  await expect(dialog).toBeVisible()
  await expect(dialog.getByText(/已存在，不能重复录入/)).toBeVisible()
})

test('未完成培训不能录入 POST 成绩', async ({ page }) => {
  await login(page, 'hr.demo')
  await gotoSnapshot(page, '/operations/tests', 'FLOW-SNAPSHOT-05')

  await page.getByRole('button', { name: '录入成绩' }).click()
  await page.getByRole('combobox', { name: '员工' }).click()
  await page.getByRole('option', { name: /张三/ }).click()
  await page.getByRole('combobox', { name: '课程' }).click()
  await page.getByRole('option', { name: /数据库性能优化实战/ }).click()
  await page.getByRole('dialog').getByText('训后测试', { exact: true }).click()
  await page.getByRole('button', { name: '提交' }).click()

  const dialog = page.getByRole('dialog')
  await expect(dialog.getByText(/尚未完成培训/)).toBeVisible()
})

test('无有效报名不能录入 PRE 成绩', async ({ page }) => {
  await login(page, 'hr.demo')
  await gotoSnapshot(page, '/operations/tests', 'FLOW-SNAPSHOT-02')

  await page.getByRole('button', { name: '录入成绩' }).click()
  await page.getByRole('combobox', { name: '员工' }).click()
  await page.getByRole('option', { name: /张三/ }).click()
  await page.getByRole('combobox', { name: '课程' }).click()
  await page.getByRole('option', { name: /数据库性能优化实战/ }).click()
  await page.getByRole('button', { name: '提交' }).click()

  await expect(page.getByRole('dialog').getByText(/没有有效报名/)).toBeVisible()
})

test('普通员工访问测试成绩管理被拒绝', async ({ page }) => {
  await login(page, 'employee.demo')
  await page.goto('/operations/tests')
  await expect(page).toHaveURL(/\/403$/)
  await expect(page.getByRole('heading', { name: '无权访问' })).toBeVisible()
})

test('员工在「我的成绩」查看本人训前训后成绩与提升率', async ({ page }) => {
  await login(page, 'employee.demo')
  await gotoSnapshot(page, '/my/tests', 'FLOW-SNAPSHOT-05')
  await expect(page.getByRole('heading', { name: '我的成绩', exact: true })).toBeVisible()

  await expect(page.getByRole('cell', { name: '数据库性能优化实战', exact: true })).toBeVisible()
  await expect(page.getByLabel('状态：78 分')).toBeVisible()
  await expect(page.getByLabel('状态：85 分')).toBeVisible()
  await expect(page.getByRole('cell', { name: '+7', exact: true })).toBeVisible()
  await expect(page.getByRole('cell', { name: '+9%', exact: true })).toBeVisible()
  await expect(page.getByRole('button', { name: '录入成绩' })).toHaveCount(0)
})

test('员工侧边栏提供「我的成绩」入口并可进入', async ({ page }) => {
  await login(page, 'employee.demo')
  const sidebar = page.locator('.sidebar-nav')
  await sidebar.getByText('我的培训', { exact: true }).click()
  await sidebar.getByText('我的成绩', { exact: true }).click()

  await expect(page).toHaveURL(/\/my\/tests$/)
  await expect(page.getByRole('heading', { name: '我的成绩', exact: true })).toBeVisible()
})
