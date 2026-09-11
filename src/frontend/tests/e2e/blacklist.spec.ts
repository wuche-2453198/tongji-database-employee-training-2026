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

test('主管候选员工仅限本部门且不含管理账号', async ({ page }) => {
  await login(page, 'manager.demo')
  await page.goto('/organization/blacklist')
  await page.getByRole('button', { name: '加入黑名单' }).click()
  await page.getByRole('combobox', { name: '员工' }).click()

  // 本部门普通员工可选
  await expect(page.getByRole('option', { name: /王五/ })).toBeVisible()
  // 其他部门员工不可见
  await expect(page.getByRole('option', { name: /钱七/ })).toHaveCount(0)
  // 管理员 / HR / 部门主管账号不可选
  await expect(page.getByRole('option', { name: /赵管理员/ })).toHaveCount(0)
  await expect(page.getByRole('option', { name: /王 HR/ })).toHaveCount(0)
  await expect(page.getByRole('option', { name: /李主管/ })).toHaveCount(0)
})

test('重复加入黑名单给出明确冲突提示', async ({ page }) => {
  await login(page, 'manager.demo')
  await page.goto('/organization/blacklist')
  await page.getByRole('button', { name: '加入黑名单' }).click()
  await page.getByRole('combobox', { name: '员工' }).click()
  // 张三(55) 在本部门且已处于生效中黑名单
  await page.getByRole('option', { name: /张三/ }).click()
  await page.getByPlaceholder('请填写加入黑名单的原因').fill('重复加入')
  await page.getByRole('button', { name: '确认加入' }).click()

  await expect(page.getByRole('dialog')).toBeVisible()
  await expect(page.getByRole('dialog').getByText(/已在黑名单中/)).toBeVisible()
})

test('加入黑名单原因必填', async ({ page }) => {
  await login(page, 'manager.demo')
  await page.goto('/organization/blacklist')
  await page.getByRole('button', { name: '加入黑名单' }).click()
  await page.getByRole('combobox', { name: '员工' }).click()
  await page.getByRole('option', { name: /王五/ }).click()
  await page.getByRole('button', { name: '确认加入' }).click()

  await expect(page.getByRole('dialog').getByText('请填写黑名单原因。')).toBeVisible()
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
