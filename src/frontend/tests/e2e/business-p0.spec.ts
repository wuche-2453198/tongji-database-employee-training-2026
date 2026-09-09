import { expect, test, type Browser, type Page } from '@playwright/test'

async function login(page: Page, account: string) {
  await page.goto('/login')
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

async function newContextPage(browser: Browser, account: string) {
  const context = await browser.newContext()
  const page = await context.newPage()
  await login(page, account)
  return { context, page }
}

test('B3 主管审批与 HR 备案主流程', async ({ browser }) => {
  const manager = await newContextPage(browser, 'manager.demo')
  await manager.page.goto('/approvals/department?mockSnapshot=FLOW-SNAPSHOT-02')
  await loadSnapshot(manager.page, 'FLOW-SNAPSHOT-02')
  await expect(manager.page.getByRole('heading', { name: '主管审批', exact: true })).toBeVisible()
  await expect(manager.page.getByText('数据库性能优化实战')).toBeVisible()
  await manager.page.setViewportSize({ width: 1440, height: 900 })
  await manager.page.screenshot({
    path: 'test-results/b3-manager-approval-1440.png',
    fullPage: true,
  })
  await manager.page.getByRole('button', { name: '通过' }).click()
  await manager.page.getByRole('button', { name: '确认通过' }).click()
  await expect(manager.page.getByText('暂无待处理申请')).toBeVisible()
  await manager.context.close()

  const hr = await newContextPage(browser, 'hr.demo')
  await hr.page.goto('/filings/hr?mockSnapshot=FLOW-SNAPSHOT-03')
  await loadSnapshot(hr.page, 'FLOW-SNAPSHOT-03')
  await expect(hr.page.getByRole('heading', { name: 'HR 备案', exact: true })).toBeVisible()
  await hr.page.getByRole('button', { name: '备案' }).click()
  await hr.page.getByRole('button', { name: '确认备案' }).click()
  await expect(hr.page.getByText('暂无待备案申请')).toBeVisible()
  await hr.context.close()
})

test('B4 报名详情与签到完成状态同步', async ({ browser }) => {
  const employee = await newContextPage(browser, 'employee.demo')
  await employee.page.goto('/my/registrations?mockSnapshot=FLOW-SNAPSHOT-05')
  await loadSnapshot(employee.page, 'FLOW-SNAPSHOT-05')
  await expect(employee.page.getByRole('heading', { name: '我的报名', exact: true })).toBeVisible()
  await expect(employee.page.getByText('数据库性能优化实战')).toBeVisible()
  await employee.page.setViewportSize({ width: 1024, height: 768 })
  await employee.page.screenshot({
    path: 'test-results/b4-my-registration-1024.png',
    fullPage: true,
  })
  await employee.page.getByRole('button', { name: '查看详情' }).click()
  await expect(employee.page.getByRole('heading', { name: '报名详情', exact: true })).toBeVisible()
  await expect(employee.page.getByLabel('状态：已报名')).toBeVisible()
  await employee.context.close()

  const hr = await newContextPage(browser, 'hr.demo')
  await hr.page.goto('/operations/attendance?mockSnapshot=FLOW-SNAPSHOT-05')
  await loadSnapshot(hr.page, 'FLOW-SNAPSHOT-05')
  await hr.page.getByRole('button', { name: '签到' }).click()
  await hr.page.getByRole('button', { name: '确认' }).click()
  await expect(hr.page.getByLabel('状态：已签到')).toBeVisible()
  await hr.page.getByRole('button', { name: '完成' }).click()
  await hr.page.getByRole('button', { name: '确认' }).click()
  await expect(hr.page.getByLabel('状态：已完成')).toBeVisible()
  await hr.context.close()
})

test('B5 证书候选生成与结果未知异常路径', async ({ browser }) => {
  const hr = await newContextPage(browser, 'hr.demo')
  await hr.page.goto('/operations/certificates?mockSnapshot=FLOW-SNAPSHOT-07')
  await loadSnapshot(hr.page, 'FLOW-SNAPSHOT-07')
  await expect(hr.page.getByRole('heading', { name: '证书管理', exact: true })).toBeVisible()
  await expect(hr.page.getByText('可生成')).toBeVisible()
  await hr.page.setViewportSize({ width: 1440, height: 900 })
  await hr.page.screenshot({
    path: 'test-results/b5-certificate-management-1440.png',
    fullPage: true,
  })
  await hr.page.getByRole('button', { name: '生成证书' }).click()
  await hr.page.getByRole('button', { name: '确认生成' }).click()
  await expect(hr.page).toHaveURL(/\/certificates\/10002$/)
  await expect(hr.page.getByText('培训证书')).toBeVisible()
  await hr.context.close()

  const employee = await newContextPage(browser, 'employee.demo')
  await employee.page.goto('/my/certificates?mockSnapshot=FLOW-SNAPSHOT-08')
  await loadSnapshot(employee.page, 'FLOW-SNAPSHOT-08')
  await expect(employee.page.getByRole('heading', { name: '我的证书', exact: true })).toBeVisible()
  await expect(employee.page.getByText('CERT-2026-0001')).toBeVisible()
  await employee.page.goto(
    '/operations/certificates?scenario=forbidden&mockSnapshot=FLOW-SNAPSHOT-07',
  )
  await expect(employee.page).toHaveURL(/\/403$/)
  await employee.context.close()
})
