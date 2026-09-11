import { expect, test, type Page } from '@playwright/test'

async function login(page: Page, account: string) {
  await page.goto('/login')
  await page.getByLabel('员工账号').fill(account)
  await page.getByLabel('密码').fill('Demo@123')
  await page.getByRole('button', { name: '登录系统' }).click()
  await expect(page).toHaveURL(/\/dashboard$/)
}

// 回归：PageHeader 的 #action 插槽在 v-if 为假时只渲染注释节点，
// 修复前会回退渲染一个无文字、无 tooltip 的蓝色空白按钮。
test('HR 在部门预算页不出现空白操作按钮', async ({ page }) => {
  await login(page, 'hr.demo')
  await page.goto('/organization/budgets')
  await expect(page.getByRole('heading', { name: '部门预算管理' })).toBeVisible()

  const actionButtons = page.locator('.page-header__action button')
  await expect(actionButtons).toHaveCount(0)
})

test('管理员在部门预算页可见「新增预算」按钮', async ({ page }) => {
  await login(page, 'admin.demo')
  await page.goto('/organization/budgets')
  await expect(page.getByRole('heading', { name: '部门预算管理' })).toBeVisible()

  const actionButtons = page.locator('.page-header__action button')
  await expect(actionButtons).toHaveCount(1)
  await expect(actionButtons.first()).toHaveText('新增预算')
})
