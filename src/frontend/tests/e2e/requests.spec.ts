import { expect, test, type Page } from '@playwright/test'

async function login(page: Page, account = 'employee.demo') {
  await page.goto('/login')
  if (!page.url().endsWith('/login')) {
    await expect(page).toHaveURL(/\/dashboard$/)
    return
  }
  await page.getByLabel('员工账号').fill(account)
  await page.getByLabel('密码').fill('Demo@123')
  await page.getByRole('button', { name: '登录系统' }).click()
  await expect(page).toHaveURL(/\/dashboard$/)
}

async function loadSnapshot(page: Page, snapshot: string) {
  await page.evaluate((value) => {
    ;(window as unknown as { __trainingMockLoad?: (id: string) => void }).__trainingMockLoad?.(
      value,
    )
  }, snapshot)
  await page.reload()
}

test('员工可以从课程上下文提交申请并在我的申请查看详情', async ({ page }) => {
  await login(page)
  await page.setViewportSize({ width: 1024, height: 768 })
  await page.goto('/my/requests/new?courseId=4001&mockSnapshot=FLOW-SNAPSHOT-01')
  await loadSnapshot(page, 'FLOW-SNAPSHOT-01')
  await expect(page.getByRole('heading', { name: '发起培训申请', exact: true })).toBeVisible()
  await expect(page.getByRole('heading', { name: '数据库性能优化实战', exact: true })).toBeVisible()
  await page.screenshot({ path: 'test-results/request-create-1024.png', fullPage: true })
  await page.getByRole('button', { name: '提交申请' }).click()
  await expect(page.getByText('请填写申请理由。')).toBeVisible()
  await expect(page.getByLabel('申请理由')).toBeFocused()
  await page.getByLabel('申请理由').fill('用于验证数据库课程申请流程')
  await page.getByRole('button', { name: '提交申请' }).click()
  await expect(page).toHaveURL(/\/my\/requests$/)
  await expect(page.getByText('培训申请已提交')).toBeVisible()
  await expect(page.getByText('数据库性能优化实战')).toBeVisible()
  await page.getByRole('button', { name: '查看详情' }).click()
  await expect(page).toHaveURL(/\/requests\/5001$/)
  await expect(page.getByText('培训申请 5001')).toBeVisible()
  await expect(page.getByText('部门主管审批')).toBeVisible()
  await page.setViewportSize({ width: 1440, height: 900 })
  await page.screenshot({ path: 'test-results/request-detail-1440.png', fullPage: true })
})

test('我的申请支持空态、无结果、失败与 URL 筛选恢复', async ({ page }) => {
  await login(page)
  await page.goto('/my/requests?mockSnapshot=FLOW-SNAPSHOT-01')
  await expect(page.getByRole('heading', { name: '暂无培训申请' })).toBeVisible()
  await page.goto('/my/requests?mockSnapshot=FLOW-SNAPSHOT-02&keyword=不存在')
  await expect(page.getByRole('heading', { name: '未找到匹配申请' })).toBeVisible()
  await page.goto('/my/requests?scenario=failure&mockSnapshot=FLOW-SNAPSHOT-02')
  await expect(page.getByRole('heading', { name: '数据加载失败' })).toBeVisible()
  await page.goto('/my/requests?mockSnapshot=FLOW-SNAPSHOT-02')
  await page.locator('.el-select').first().click()
  await page.getByRole('option', { name: '待审批' }).click()
  await page.getByRole('button', { name: '查询' }).click()
  await expect(page).toHaveURL(/status=PENDING/)
  await page.reload()
  await expect(page.getByText('数据库性能优化实战')).toBeVisible()
})

test('申请详情覆盖四状态、403、404以及角色访问边界', async ({ browser, page }) => {
  await login(page)
  for (const [snapshot, label] of [
    ['FLOW-SNAPSHOT-02', '待审批'],
    ['FLOW-SNAPSHOT-03', '部门已通过'],
    ['FLOW-SNAPSHOT-04', '已备案'],
  ] as const) {
    await page.goto(`/requests/5001?mockSnapshot=${snapshot}`)
    await loadSnapshot(page, snapshot)
    await expect(page.getByLabel(`状态：${label}`)).toBeVisible()
  }
  await page.goto('/requests/5001?scenario=forbidden')
  await expect(page.getByRole('heading', { name: '页面无权限' })).toBeVisible()
  await page.goto('/requests/9999?scenario=not-found')
  await expect(
    page
      .getByRole('heading', { name: '页面无权限' })
      .or(page.getByRole('heading', { name: '页面或资源不存在' })),
  ).toBeVisible()

  const managerContext = await browser.newContext()
  const managerPage = await managerContext.newPage()
  await login(managerPage, 'manager.demo')
  await managerPage.goto('/requests/5001?mockSnapshot=FLOW-SNAPSHOT-02')
  await expect(managerPage.getByText('培训申请 5001')).toBeVisible()
  await managerContext.close()

  const adminContext = await browser.newContext()
  const adminPage = await adminContext.newPage()
  await login(adminPage, 'admin.demo')
  await adminPage.goto('/my/requests')
  await expect(adminPage).toHaveURL(/\/403$/)
  await adminContext.close()
})

test('重复申请被服务拒绝，结果未知先查询最终状态', async ({ page }) => {
  await login(page)
  await page.goto('/my/requests/new?courseId=4001&mockSnapshot=FLOW-SNAPSHOT-02')
  await loadSnapshot(page, 'FLOW-SNAPSHOT-02')
  await expect(page.getByRole('button', { name: '提交申请' })).toBeDisabled()

  await page.goto(
    '/my/requests/new?courseId=4001&scenario=result-unknown&mockSnapshot=FLOW-SNAPSHOT-01',
  )
  await loadSnapshot(page, 'FLOW-SNAPSHOT-01')
  await page.getByLabel('申请理由').fill('验证结果未知后的最终状态查询')
  await page.getByRole('button', { name: '提交申请' }).click()
  await expect(page).toHaveURL(/\/my\/requests$/)
  await expect(page.getByText('数据库性能优化实战')).toBeVisible()
})
