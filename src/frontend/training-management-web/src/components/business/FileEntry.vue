<script setup lang="ts">
import { Clock, Document, Lock } from '@element-plus/icons-vue'
import { computed } from 'vue'

import AppButton from '@/components/common/AppButton.vue'

type FileEntryState = 'available' | 'expired' | 'forbidden'

const props = withDefaults(
  defineProps<{
    title: string
    description?: string
    state?: FileEntryState
  }>(),
  {
    description: '',
    state: 'available',
  },
)

const emit = defineEmits<{
  open: []
}>()

const visibleTitle = computed(() => (props.state === 'forbidden' ? '受限资源' : props.title))
const stateDescription = computed(() => {
  if (props.state === 'expired') return props.description || '资源链接已失效，请联系培训负责人。'
  if (props.state === 'forbidden') return '当前角色无权查看此资源。'
  return props.description || '资源可用，打开后将在新窗口中查看。'
})
const icon = computed(() => {
  if (props.state === 'expired') return Clock
  if (props.state === 'forbidden') return Lock
  return Document
})
</script>

<template>
  <article class="file-entry" :class="`file-entry--${state}`">
    <el-icon class="file-entry__icon" :size="24" aria-hidden="true">
      <component :is="icon" />
    </el-icon>
    <div class="file-entry__copy">
      <h3>{{ visibleTitle }}</h3>
      <p>{{ stateDescription }}</p>
    </div>
    <AppButton
      label="打开资源"
      variant="text"
      :disabled="state !== 'available'"
      :disabled-reason="
        state === 'expired' ? '链接已失效' : state === 'forbidden' ? '无查看权限' : ''
      "
      @click="emit('open')"
    />
  </article>
</template>

<style scoped>
.file-entry {
  display: grid;
  min-height: 80px;
  grid-template-columns: auto 1fr auto;
  align-items: center;
  gap: var(--space-4);
  padding: var(--space-4);
  border: var(--border-default);
  border-radius: var(--radius-lg);
  background: var(--color-surface);
}

.file-entry__icon {
  color: var(--color-primary);
}

.file-entry--expired .file-entry__icon {
  color: var(--color-warning);
}

.file-entry--forbidden .file-entry__icon {
  color: var(--color-error);
}

.file-entry__copy {
  min-width: 0;
}

.file-entry h3 {
  margin: 0;
  overflow: hidden;
  font-size: var(--font-size-body);
  font-weight: 500;
  line-height: var(--line-height-body);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.file-entry p {
  margin: var(--space-1) 0 0;
  color: var(--text-secondary);
  font-size: var(--font-size-caption);
  line-height: var(--line-height-caption);
}
</style>
