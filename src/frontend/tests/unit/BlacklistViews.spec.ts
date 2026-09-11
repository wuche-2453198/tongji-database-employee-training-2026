import { nextTick } from 'vue'
import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import ElementPlus from 'element-plus'

import AppButton from '@/components/common/AppButton.vue'
import BlacklistManagementView from '@/views/blacklist/BlacklistManagementView.vue'
import { resetMockBusinessSnapshot } from '@/mocks/repositories/business-repository'
import { useAuthStore } from '@/stores/auth'

async function loginAs(account: string): Promise<void> {
  window.sessionStorage.setItem(
    'training-management.mock-auth-session',
    JSON.stringify({ account, expiresAt: Date.now() + 3600_000 }),
  )
  await useAuthStore().initialize()
}

function buttonLabels(wrapper: ReturnType<typeof mount>): string[] {
  return wrapper.findAllComponents(AppButton).map((button) => String(button.props('label')))
}

async function mountView() {
  const wrapper = mount(BlacklistManagementView, {
    global: { plugins: [ElementPlus] },
  })
  await new Promise((resolve) => setTimeout(resolve, 300))
  await nextTick()
  return wrapper
}

describe('黑名单管理页面', () => {
  it('主管看到本部门黑名单记录与「加入黑名单」入口', async () => {
    setActivePinia(createPinia())
    await loginAs('manager.demo')
    resetMockBusinessSnapshot()
    const wrapper = await mountView()
    expect(buttonLabels(wrapper)).toContain('加入黑名单')
    expect(wrapper.text()).toContain('张三')
    expect(wrapper.text()).toContain('技术部')
    expect(wrapper.text()).not.toContain('钱七')
    wrapper.unmount()
  })

  it('管理员看到全部黑名单记录与「加入黑名单」入口', async () => {
    setActivePinia(createPinia())
    await loginAs('admin.demo')
    resetMockBusinessSnapshot()
    const wrapper = await mountView()
    expect(buttonLabels(wrapper)).toContain('加入黑名单')
    expect(wrapper.text()).toContain('张三')
    expect(wrapper.text()).toContain('钱七')
    wrapper.unmount()
  })

  it('普通员工没有「加入黑名单」入口', async () => {
    setActivePinia(createPinia())
    await loginAs('employee.demo')
    resetMockBusinessSnapshot()
    const wrapper = await mountView()
    expect(buttonLabels(wrapper)).not.toContain('加入黑名单')
    wrapper.unmount()
  })

  it('「加入黑名单」对话框包含员工、原因、起止日期与操作按钮', async () => {
    setActivePinia(createPinia())
    await loginAs('manager.demo')
    resetMockBusinessSnapshot()
    const wrapper = mount(BlacklistManagementView, {
      global: { plugins: [ElementPlus] },
      attachTo: document.body,
    })
    await new Promise((resolve) => setTimeout(resolve, 300))
    await nextTick()

    const addButton = wrapper
      .findAllComponents(AppButton)
      .find((button) => button.props('label') === '加入黑名单')
    // AppButton 的根节点是 span，真正的点击监听在内层 el-button 上。
    await addButton?.find('button').trigger('click')
    await new Promise((resolve) => setTimeout(resolve, 300))
    await nextTick()

    // el-dialog 通过 teleport 渲染到 body，需从文档而非组件 wrapper 中查找。
    const dialog = [...document.querySelectorAll('.el-dialog')].find((node) =>
      node.textContent?.includes('黑名单原因'),
    ) as HTMLElement | undefined
    expect(dialog).toBeTruthy()
    expect(dialog?.textContent).toContain('员工')
    expect(dialog?.textContent).toContain('开始日期')
    expect(dialog?.textContent).toContain('结束日期')
    expect(dialog?.textContent).toContain('取消')
    expect(dialog?.textContent).toContain('确认加入')

    const reason = dialog?.querySelector('textarea')
    expect(reason?.getAttribute('placeholder')).toBe('请填写加入黑名单的原因')
    expect(dialog?.querySelectorAll('.el-date-editor')).toHaveLength(2)

    wrapper.unmount()
  })
})
