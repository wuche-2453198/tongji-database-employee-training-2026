import { nextTick } from 'vue'
import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import ElementPlus from 'element-plus'
import { createMemoryHistory, createRouter } from 'vue-router'

import CourseListView from '@/views/courses/CourseListView.vue'
import CourseDetailView from '@/views/courses/CourseDetailView.vue'

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
