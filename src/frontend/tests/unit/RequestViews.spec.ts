import { nextTick } from 'vue'
import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import ElementPlus from 'element-plus'
import { createMemoryHistory, createRouter } from 'vue-router'

import MyRequestListView from '@/views/requests/MyRequestListView.vue'
import RequestDetailView from '@/views/requests/RequestDetailView.vue'
import { resetMockBusinessSnapshot } from '@/mocks/repositories/business-repository'

async function testRouter(path: string) {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/my/requests', component: MyRequestListView },
      { path: '/requests/:id', component: RequestDetailView },
    ],
  })
  await router.push(path)
  await router.isReady()
  return router
}

describe('B2 培训申请页面', () => {
  it('我的申请列表显示共享快照中的待审批记录', async () => {
    setActivePinia(createPinia())
    window.sessionStorage.setItem(
      'training-management.mock-auth-session',
      JSON.stringify({ account: 'employee.demo' }),
    )
    resetMockBusinessSnapshot('FLOW-SNAPSHOT-02')
    const router = await testRouter('/my/requests')
    const wrapper = mount(MyRequestListView, { global: { plugins: [ElementPlus, router] } })
    await new Promise((resolve) => setTimeout(resolve, 300))
    await nextTick()
    expect(wrapper.text()).toContain('数据库性能优化实战')
    expect(wrapper.text()).toContain('待审批')
    wrapper.unmount()
  })

  it('申请详情按路由 id 重新获取并呈现时间线', async () => {
    setActivePinia(createPinia())
    window.sessionStorage.setItem(
      'training-management.mock-auth-session',
      JSON.stringify({ account: 'employee.demo' }),
    )
    resetMockBusinessSnapshot('FLOW-SNAPSHOT-02')
    const router = await testRouter('/requests/5001')
    const wrapper = mount(RequestDetailView, { global: { plugins: [ElementPlus, router] } })
    await new Promise((resolve) => setTimeout(resolve, 300))
    await nextTick()
    expect(wrapper.text()).toContain('培训申请 5001')
    expect(wrapper.text()).toContain('部门主管审批')
    wrapper.unmount()
  })
})
