import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'

import FoundationView from '@/views/dashboard/FoundationView.vue'

describe('FoundationView', () => {
  it('展示工程就绪状态', () => {
    const wrapper = mount(FoundationView)

    expect(wrapper.get('h1').text()).toBe('企业内部培训管理系统')
    expect(wrapper.text()).toContain('前端基础工程已就绪')
    expect(wrapper.text()).toContain('Mock')
  })
})
