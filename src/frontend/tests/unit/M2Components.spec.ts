import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'

import CourseSummary from '@/components/business/CourseSummary.vue'
import FileEntry from '@/components/business/FileEntry.vue'
import PageHeader from '@/components/common/PageHeader.vue'
import PageState from '@/components/common/PageState.vue'
import SearchPanel from '@/components/common/SearchPanel.vue'
import StatusTag from '@/components/common/StatusTag.vue'

describe('M2 公共组件', () => {
  it('状态标签同时提供中文文本和可访问名称', () => {
    const wrapper = mount(StatusTag, {
      props: { label: '待审批', semantic: 'warning' },
    })

    expect(wrapper.get('[role="status"]').attributes('aria-label')).toBe('状态：待审批')
    expect(wrapper.text()).toContain('待审批')
    expect(wrapper.classes()).toContain('status-tag--warning')
  })

  it('加载失败状态展示 traceId 并触发恢复动作', async () => {
    const wrapper = mount(PageState, {
      props: { state: 'error', traceId: 'TRACE-001', compact: true },
    })

    expect(wrapper.get('[role="alert"]').text()).toContain('数据加载失败')
    expect(wrapper.text()).toContain('TRACE-001')
    await wrapper.get('button').trigger('click')
    expect(wrapper.emitted('primary')).toHaveLength(1)
  })

  it('搜索区支持提交、重置和展开状态', async () => {
    const wrapper = mount(SearchPanel, {
      props: { expanded: false },
      slots: { default: '<input aria-label="课程名称" />', expanded: '<input />' },
    })

    await wrapper.get('form').trigger('submit')
    expect(wrapper.emitted('search')).toHaveLength(1)

    const buttons = wrapper.findAll('button')
    await buttons[0]?.trigger('click')
    expect(wrapper.emitted('update:expanded')?.[0]).toEqual([true])
    await buttons[2]?.trigger('click')
    expect(wrapper.emitted('reset')).toHaveLength(1)
  })

  it('课程满员状态只展示传入的服务端剩余名额', () => {
    const wrapper = mount(CourseSummary, {
      props: {
        state: 'full',
        course: {
          name: '数据库基础训练',
          type: '专业技能',
          trainer: '李老师',
          schedule: '2026-09-12 09:00',
          location: '培训室 A301',
          hours: 8,
          remainingSeats: 0,
        },
      },
    })

    expect(wrapper.text()).toContain('名额已满')
    expect(wrapper.text()).toContain('剩余名额：0')
  })

  it('无权资源不暴露原始资源名称', () => {
    const wrapper = mount(FileEntry, {
      props: { title: '保密薪酬培训资料.pdf', state: 'forbidden' },
    })

    expect(wrapper.text()).toContain('受限资源')
    expect(wrapper.text()).not.toContain('保密薪酬培训资料.pdf')
    expect(wrapper.get('button').attributes()).toHaveProperty('disabled')
  })

  it('PageHeader 操作区在插槽渲染为空时不产生空白按钮', () => {
    const wrapper = mount(PageHeader, {
      props: { title: '部门预算管理' },
      slots: { action: '<template v-if="false">隐藏</template>' },
    })

    expect(wrapper.find('.page-header__action').exists()).toBe(true)
    expect(wrapper.findAll('.page-header__action button')).toHaveLength(0)
  })

  it('PageHeader 提供 actionLabel 时渲染按钮并派发 action 事件', async () => {
    const wrapper = mount(PageHeader, {
      props: { title: '部门预算管理', actionLabel: '新增预算' },
    })

    const button = wrapper.get('.page-header__action button')
    expect(button.text()).toContain('新增预算')
    await button.trigger('click')
    expect(wrapper.emitted('action')).toHaveLength(1)
  })
})
