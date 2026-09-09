import { expect, test, type Page } from '@playwright/test'

async function resetSnapshot(page: Page) {
  await page.addInitScript(() => {
    window.sessionStorage.removeItem('training-management.mock-business-state')
  })
}

test.beforeEach(async ({ page }) => {
  await resetSnapshot(page)
})

test('课程中心主路径支持筛选、URL同步、详情和刷新恢复', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('员工账号').fill('employee.demo')
  await page.getByLabel('密码').fill('Demo@123')
  await page.getByRole('button', { name: '登录系统' }).click()
  await expect(page).toHaveURL(/\/dashboard$/)
  await page.setViewportSize({ width: 1024, height: 768 })
  await page.goto('/courses')

  await expect(page.getByRole('heading', { name: '课程中心', exact: true })).toBeVisible()
  await expect(page.getByText('数据库性能优化实战')).toBeVisible()
  await page.getByLabel('课程关键词').fill('数据库')
  await page.getByRole('button', { name: '查询' }).click()
  await expect(page).toHaveURL(/keyword=%E6%95%B0%E6%8D%AE%E5%BA%93/)
  await expect(page.getByText('数据库性能优化实战')).toBeVisible()
  await expect(page.getByText('新任主管沟通与反馈')).toHaveCount(0)
  await page.screenshot({ path: 'test-results/course-list-1024.png', fullPage: true })
  await page.getByRole('button', { name: '数据库性能优化实战' }).click()
  await expect(page).toHaveURL(/\/courses\/4001$/)
  await expect(page.getByText('4001', { exact: true })).toBeVisible()
  await expect(page.getByText('陈老师').first()).toBeVisible()
  await page.setViewportSize({ width: 1440, height: 900 })
  await page.screenshot({ path: 'test-results/course-detail-1440.png', fullPage: true })
  await page.reload()
  await expect(page.getByRole('heading', { name: '课程详情', exact: true })).toBeVisible()
  await page.getByRole('button', { name: '返回列表' }).click()
  await expect(page).toHaveURL(/\/courses\?/)
  await expect(page.getByLabel('课程关键词')).toHaveValue('数据库')
})

test('四角色均可访问课程列表和详情，只有员工看到业务入口', async ({ browser }) => {
  const accounts = [
    ['employee.demo', true],
    ['manager.demo', false],
    ['hr.demo', false],
    ['admin.demo', false],
  ] as const

  for (const [account, employee] of accounts) {
    const context = await browser.newContext()
    const page = await context.newPage()
    await resetSnapshot(page)
    await page.goto('/login')
    await page.getByLabel('员工账号').fill(account)
    await page.getByLabel('密码').fill('Demo@123')
    await page.getByRole('button', { name: '登录系统' }).click()
    await expect(page).toHaveURL(/\/dashboard$/)
    await page.goto('/courses')
    await expect(page.getByRole('heading', { name: '课程中心', exact: true })).toBeVisible()
    await page.goto('/courses/4001')
    await expect(page.getByRole('heading', { name: '课程详情', exact: true })).toBeVisible()
    if (employee) {
      await expect(page.getByRole('button', { name: '提交培训申请' })).toBeVisible()
    } else {
      await expect(page.getByText('当前角色仅可浏览课程详情')).toBeVisible()
    }
    await page.getByRole('button', { name: /张三|李主管|王 HR|赵管理员/ }).click()
    await page.getByRole('menuitem', { name: '退出登录' }).click()
    await context.close()
  }
})

test('课程列表区分业务空、搜索无结果与服务失败', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('员工账号').fill('employee.demo')
  await page.getByLabel('密码').fill('Demo@123')
  await page.getByRole('button', { name: '登录系统' }).click()
  await expect(page).toHaveURL(/\/dashboard$/)

  await page.goto('/courses?scenario=empty')
  await expect(page.getByRole('heading', { name: '暂无已发布课程' })).toBeVisible()
  await page.goto('/courses?keyword=不存在的课程')
  await expect(page.getByRole('heading', { name: '未找到匹配课程' })).toBeVisible()
  await page.goto('/courses?scenario=failure')
  await expect(page.getByRole('heading', { name: '数据加载失败' })).toBeVisible()

  await page.goto('/courses/4001?scenario=forbidden')
  await expect(page.getByRole('heading', { name: '页面无权限' })).toBeVisible()
  await page.goto('/courses/4001?scenario=not-found')
  await expect(page.getByRole('heading', { name: '页面或资源不存在' })).toBeVisible()
})

test('已备案课程报名成功后刷新余量和本人资格', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('员工账号').fill('employee.demo')
  await page.getByLabel('密码').fill('Demo@123')
  await page.getByRole('button', { name: '登录系统' }).click()
  await expect(page).toHaveURL(/\/dashboard$/)
  await page.goto('/courses/4001?mockSnapshot=FLOW-SNAPSHOT-04')
  await page.getByRole('button', { name: '报名课程' }).click()
  await expect(page.getByText(/报名成功，报名编号/)).toBeVisible()
  await expect(page.getByRole('button', { name: '报名课程' })).toBeDisabled()
})

test('员工在结果未知时先查询最终报名状态，冲突时刷新详情', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('员工账号').fill('employee.demo')
  await page.getByLabel('密码').fill('Demo@123')
  await page.getByRole('button', { name: '登录系统' }).click()
  await expect(page).toHaveURL(/\/dashboard$/)

  await page.goto('/courses/4001?scenario=result-unknown&mockSnapshot=FLOW-SNAPSHOT-04')
  await page.getByRole('button', { name: '报名课程' }).click()
  await expect(page.getByText('报名结果未知')).toBeVisible()

  await page.goto('/courses/4001?scenario=conflict&mockSnapshot=FLOW-SNAPSHOT-04')
  await page.getByRole('button', { name: '报名课程' }).click()
  await expect(page.getByText('课程名额已满，请刷新课程状态。')).toBeVisible()
})
