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

test('HR 从详情页发布草稿课程：确认弹窗与状态变更', async ({ page }) => {
  await login(page, 'hr.demo')
  await page.goto('/courses/4005')
  await expect(page.getByRole('button', { name: '发布课程' })).toBeVisible()

  await page.getByRole('button', { name: '发布课程' }).click()
  await expect(
    page.getByText(
      '确认发布课程「数据分析入门实战」吗？发布后将占用主办部门预算，且不可恢复为草稿。',
    ),
  ).toBeVisible()
  await page.getByRole('button', { name: '确认发布' }).click()

  await expect(page.getByText('课程已发布。')).toBeVisible()
  await expect(page.getByRole('button', { name: '发布课程' })).toHaveCount(0)
  await expect(page.getByRole('button', { name: '关闭课程' })).toBeVisible()
})

test('HR 从列表页发布草稿并关闭已发布课程', async ({ page }) => {
  await login(page, 'hr.demo')
  await page.goto('/courses')
  await expect(page.getByText('数据分析入门实战')).toBeVisible()

  // 发布草稿课程
  await page.getByRole('button', { name: '发布课程' }).click()
  await expect(
    page.getByText('确定发布课程《数据分析入门实战》吗？发布后员工将可以查看并申请。'),
  ).toBeVisible()
  await page.getByRole('button', { name: '确认发布' }).click()
  await expect(page.getByRole('button', { name: '发布课程' })).toHaveCount(0)
  await expect(page.getByRole('button', { name: '关闭课程' })).toHaveCount(5)

  // 关闭刚发布的课程（按开课时间降序排在首位）
  await page.getByRole('button', { name: '关闭课程' }).first().click()
  await expect(page.getByText(/确定关闭课程《数据分析入门实战》吗？/)).toBeVisible()
  await page.getByRole('button', { name: '确认关闭' }).click()
  await expect(page.getByRole('button', { name: '关闭课程' })).toHaveCount(4)
})

test('主管与员工看不到发布/关闭入口，草稿详情被拒绝', async ({ browser }) => {
  for (const account of ['manager.demo', 'employee.demo']) {
    const context = await browser.newContext()
    const page = await context.newPage()
    await resetSnapshot(page)
    await login(page, account)

    await page.goto('/courses')
    await expect(page.getByRole('heading', { name: '课程中心', exact: true })).toBeVisible()
    await expect(page.getByText('数据分析入门实战')).toHaveCount(0)
    await expect(page.getByRole('button', { name: '发布课程' })).toHaveCount(0)
    await expect(page.getByRole('button', { name: '关闭课程' })).toHaveCount(0)

    await page.goto('/courses/4005')
    await expect(page.getByRole('heading', { name: '页面无权限' })).toBeVisible()
    await context.close()
  }
})

test('发布失败时展示后端错误', async ({ page }) => {
  await login(page, 'hr.demo')
  await page.goto('/courses/4005')
  await expect(page.getByRole('button', { name: '发布课程' })).toBeVisible()

  await page.evaluate(() => {
    window.history.pushState({}, '', '/courses/4005?scenario=failure')
  })
  await page.getByRole('button', { name: '发布课程' }).click()
  await page.getByRole('button', { name: '确认发布' }).click()

  await expect(page.getByRole('heading', { name: '数据加载失败' })).toBeVisible()
  await expect(page.getByText('服务暂时不可用，请稍后重试。')).toBeVisible()
})

test('名额已满课程展示剩余名额为 0', async ({ page }) => {
  await login(page, 'employee.demo')
  await page.goto('/courses/4002')
  await expect(page.getByRole('heading', { name: '课程详情', exact: true })).toBeVisible()
  await expect(page.getByText('名额已满')).toBeVisible()
  await expect(page.getByText('剩余名额：0')).toBeVisible()
})
