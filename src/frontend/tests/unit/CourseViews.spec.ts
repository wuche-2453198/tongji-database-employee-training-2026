import { nextTick } from 'vue'
import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import ElementPlus from 'element-plus'
import { createMemoryHistory, createRouter } from 'vue-router'

import AppButton from '@/components/common/AppButton.vue'
import CourseListView from '@/views/courses/CourseListView.vue'
import CourseDetailView from '@/views/courses/CourseDetailView.vue'
import { resetMockBusinessSnapshot } from '@/mocks/repositories/business-repository'
import { useAuthStore } from '@/stores/auth'

async function testRouter(path: string) {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/courses', component: CourseListView },
      { path: '/courses/:id', component: CourseDetailView },
    ],
  })
  await router.push(path)
  await router.isReady()
  return router
}

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

describe('B1 课程中心页面', () => {
  it('课程列表渲染课程数据并使用服务端分页口径', async () => {
    setActivePinia(createPinia())
    const router = await testRouter('/courses')
    const wrapper = mount(CourseListView, {
      global: {
        plugins: [ElementPlus, router],
      },
    })
    await new Promise((resolve) => setTimeout(resolve, 500))
    await nextTick()
    expect(wrapper.text()).toContain('数据库性能优化实战')
    expect(wrapper.text()).toContain('课程列表')
    wrapper.unmount()
  })

  it('课程详情直接从路由 id 获取数据，并显示课程编号与讲师', async () => {
    setActivePinia(createPinia())
    const router = await testRouter('/courses/4001')
    const wrapper = mount(CourseDetailView, {
      global: {
        plugins: [ElementPlus, router],
      },
    })
    await new Promise((resolve) => setTimeout(resolve, 500))
    await nextTick()
    expect(wrapper.text()).toContain('4001')
    expect(wrapper.text()).toContain('陈老师')
    wrapper.unmount()
  })
})

describe('B1 课程发布/关闭与角色状态筛选', () => {
  it('HR 可看到草稿课程并同时拥有发布与关闭按钮', async () => {
    setActivePinia(createPinia())
    await loginAs('hr.demo')
    resetMockBusinessSnapshot()
    const router = await testRouter('/courses')
    const wrapper = mount(CourseListView, {
      global: { plugins: [ElementPlus, router] },
    })
    await new Promise((resolve) => setTimeout(resolve, 500))
    await nextTick()
    expect(wrapper.text()).toContain('数据分析入门实战')
    const labels = buttonLabels(wrapper)
    expect(labels).toContain('发布课程')
    expect(labels).toContain('关闭课程')
    wrapper.unmount()
  })

  it('管理员可看到草稿课程并拥有发布与关闭按钮', async () => {
    setActivePinia(createPinia())
    await loginAs('admin.demo')
    resetMockBusinessSnapshot()
    const router = await testRouter('/courses')
    const wrapper = mount(CourseListView, {
      global: { plugins: [ElementPlus, router] },
    })
    await new Promise((resolve) => setTimeout(resolve, 500))
    await nextTick()
    expect(wrapper.text()).toContain('数据分析入门实战')
    const labels = buttonLabels(wrapper)
    expect(labels).toContain('发布课程')
    expect(labels).toContain('关闭课程')
    wrapper.unmount()
  })

  it('普通员工仅见已发布课程且无发布/关闭按钮', async () => {
    setActivePinia(createPinia())
    await loginAs('employee.demo')
    resetMockBusinessSnapshot()
    const router = await testRouter('/courses')
    const wrapper = mount(CourseListView, {
      global: { plugins: [ElementPlus, router] },
    })
    await new Promise((resolve) => setTimeout(resolve, 500))
    await nextTick()
    expect(wrapper.text()).not.toContain('数据分析入门实战')
    const labels = buttonLabels(wrapper)
    expect(labels).not.toContain('发布课程')
    expect(labels).not.toContain('关闭课程')
    wrapper.unmount()
  })

  it('部门主管默认仅见已发布课程且无发布/关闭按钮', async () => {
    setActivePinia(createPinia())
    await loginAs('manager.demo')
    resetMockBusinessSnapshot()
    const router = await testRouter('/courses')
    const wrapper = mount(CourseListView, {
      global: { plugins: [ElementPlus, router] },
    })
    await new Promise((resolve) => setTimeout(resolve, 500))
    await nextTick()
    expect(wrapper.text()).not.toContain('数据分析入门实战')
    const labels = buttonLabels(wrapper)
    expect(labels).not.toContain('发布课程')
    expect(labels).not.toContain('关闭课程')
    wrapper.unmount()
  })

  it('HR 在草稿详情页看到发布按钮，在已发布详情页看到关闭按钮', async () => {
    setActivePinia(createPinia())
    await loginAs('hr.demo')
    resetMockBusinessSnapshot()
    const router = await testRouter('/courses/4005')
    const wrapper = mount(CourseDetailView, {
      global: { plugins: [ElementPlus, router] },
    })
    await new Promise((resolve) => setTimeout(resolve, 500))
    await nextTick()
    expect(buttonLabels(wrapper)).toContain('发布课程')
    expect(buttonLabels(wrapper)).not.toContain('关闭课程')
    wrapper.unmount()

    const router2 = await testRouter('/courses/4001')
    const wrapper2 = mount(CourseDetailView, {
      global: { plugins: [ElementPlus, router2] },
    })
    await new Promise((resolve) => setTimeout(resolve, 500))
    await nextTick()
    expect(buttonLabels(wrapper2)).toContain('关闭课程')
    expect(buttonLabels(wrapper2)).not.toContain('发布课程')
    wrapper2.unmount()
  })

  it('普通员工在已发布详情页没有关闭按钮', async () => {
    setActivePinia(createPinia())
    await loginAs('employee.demo')
    resetMockBusinessSnapshot()
    const router = await testRouter('/courses/4001')
    const wrapper = mount(CourseDetailView, {
      global: { plugins: [ElementPlus, router] },
    })
    await new Promise((resolve) => setTimeout(resolve, 500))
    await nextTick()
    expect(buttonLabels(wrapper)).not.toContain('关闭课程')
    expect(buttonLabels(wrapper)).not.toContain('发布课程')
    wrapper.unmount()
  })
})
