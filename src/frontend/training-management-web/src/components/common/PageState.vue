<script setup lang="ts">
import { computed } from 'vue'
import { CircleCloseFilled, Lock, Search, WarningFilled } from '@element-plus/icons-vue'

import AppButton from './AppButton.vue'

type PageStateType = 'loading' | 'empty' | 'no-result' | 'error' | 'forbidden' | 'not-found'

const props = withDefaults(
  defineProps<{
    state: PageStateType
    title?: string
    description?: string
    traceId?: string
    primaryLabel?: string
    secondaryLabel?: string
    compact?: boolean
    hidePrimary?: boolean
  }>(),
  {
    title: '',
    description: '',
    traceId: '',
    primaryLabel: '',
    secondaryLabel: '',
    compact: false,
    hidePrimary: false,
  },
)

const emit = defineEmits<{
  primary: []
  secondary: []
}>()

const defaults = computed(() => {
  const values = {
    empty: ['暂无数据', '当前还没有可展示的记录。', ''],
    'no-result': ['未找到匹配结果', '请调整关键词、状态或日期范围。', '清空筛选'],
    error: ['数据加载失败', '暂时无法获取页面数据，请稍后重试。', '重新加载'],
    forbidden: ['页面无权限', '当前角色无权访问此页面。', '返回首页'],
    'not-found': ['页面或资源不存在', '请检查地址，或返回上一层页面。', '返回首页'],
    loading: ['正在加载', '正在获取最新数据，请稍候。', ''],
  } as const

  return values[props.state]
})

const resolvedTitle = computed(() => props.title || defaults.value[0])
const resolvedDescription = computed(() => props.description || defaults.value[1])
const resolvedPrimaryLabel = computed(() =>
  props.hidePrimary ? '' : props.primaryLabel || defaults.value[2],
)
const icon = computed(() => {
  if (props.state === 'forbidden') return Lock
  if (props.state === 'not-found') return Search
  if (props.state === 'error') return CircleCloseFilled
  return WarningFilled
})
</script>

<template>
  <section
    class="page-state"
    :class="{ 'page-state--compact': compact }"
    :aria-live="state === 'loading' ? 'polite' : undefined"
    :role="state === 'error' ? 'alert' : 'status'"
  >
    <template v-if="state === 'loading'">
      <span class="sr-only">{{ resolvedTitle }}</span>
      <el-skeleton :rows="compact ? 3 : 6" animated />
    </template>

    <template v-else>
      <el-icon class="page-state__icon" :size="compact ? 40 : 56" aria-hidden="true">
        <component :is="icon" />
      </el-icon>
      <h2>{{ resolvedTitle }}</h2>
      <p>{{ resolvedDescription }}</p>
      <p v-if="traceId && state === 'error'" class="page-state__trace">问题编号：{{ traceId }}</p>
      <div v-if="resolvedPrimaryLabel || secondaryLabel" class="page-state__actions">
        <AppButton
          v-if="resolvedPrimaryLabel"
          :label="resolvedPrimaryLabel"
          variant="primary"
          @click="emit('primary')"
        />
        <AppButton v-if="secondaryLabel" :label="secondaryLabel" @click="emit('secondary')" />
      </div>
    </template>
  </section>
</template>

<style scoped>
.page-state {
  display: grid;
  min-height: 300px;
  place-items: center;
  align-content: center;
  padding: var(--space-10);
  border: var(--border-default);
  border-radius: var(--radius-lg);
  background: var(--color-surface);
  text-align: center;
}

.page-state--compact {
  min-height: 220px;
  padding: var(--space-6);
}

.page-state__icon {
  margin-bottom: var(--space-4);
  color: var(--color-neutral);
}

.page-state[role='alert'] .page-state__icon {
  color: var(--color-error);
}

.page-state h2 {
  margin: 0;
  font-size: var(--font-size-title-component);
  line-height: var(--line-height-title-component);
}

.page-state p {
  max-width: 520px;
  margin: var(--space-2) 0 0;
  color: var(--text-secondary);
  line-height: var(--line-height-body);
}

.page-state__trace {
  font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
  font-size: var(--font-size-caption);
}

.page-state__actions {
  display: flex;
  gap: var(--space-2);
  margin-top: var(--space-5);
}
</style>
