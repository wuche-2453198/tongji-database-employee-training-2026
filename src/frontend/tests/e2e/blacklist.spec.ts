import { expect, test, type Page } from '@playwright/test'

async function resetSnapshot(page: Page) {
  await page.addInitScript(() => {
    window.sessionStorage.removeItem('training-management.mock-business-state')
  })
}

async function login(page: Page, account: string) {
  await page.goto('/login')
  await page.getByLabel('员工账号').fill(account)
  await page.getByLabel('密码').fill('Demo@123')
  await page.getByRole('button', { name: '登录系统' }).click()
  await expect(page).toHaveURL(/\/dashboard$/)
}

test.beforeEach(async ({ page }) => {
  await resetSnapshot(page)
})

test('主管仅见本部门黑名单，可将本部门员工加入黑名单', async ({ page }) => {
  await login(page, 'manager.demo')
  await page.goto('/organization/blacklist')
  await expect(page.getByRole('heading', { name: '黑名单管理', exact: true })).toBeVisible()
  await expect(page.getByText('张三')).toBeVisible()
  await expect(page.getByText('钱七')).toHaveCount(0)

  await page.getByRole('button', { name: '加入黑名单' }).click()
  await page.getByRole('combobox', { name: '员工' }).click()
  await page.getByRole('option', { name: /王五/ }).click()
  await page.getByPlaceholder('请填写加入黑名单的原因').fill('违反培训纪律')
  await page.getByRole('button', { name: '确认加入' }).click()

  await expect(page.getByText('已将员工加入黑名单。')).toBeVisible()
  await expect(page.getByRole('cell', { name: '王五', exact: true })).toBeVisible()
})

test('管理员可见全部黑名单记录', async ({ page }) => {
  await login(page, 'admin.demo')
  await page.goto('/organization/blacklist')
  await expect(page.getByRole('heading', { name: '黑名单管理', exact: true })).toBeVisible()
  await expect(page.getByText('张三')).toBeVisible()
  await expect(page.getByText('钱七')).toBeVisible()
})

test('普通员工访问黑名单页被拒绝', async ({ page }) => {
  await login(page, 'employee.demo')
  await page.goto('/organization/blacklist')
  await expect(page).toHaveURL(/\/403$/)
  await expect(page.getByRole('heading', { name: '无权访问' })).toBeVisible()
})
