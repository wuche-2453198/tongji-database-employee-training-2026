<script setup lang="ts">
import type { DescriptionItem } from '@/types/ui'

withDefaults(
  defineProps<{
    items: DescriptionItem[]
    columns?: 1 | 2
    title?: string
  }>(),
  {
    columns: 2,
    title: '',
  },
)

function displayValue(value: DescriptionItem['value']): string {
  if (value === null || value === undefined || value === '') return '—'
  return String(value)
}
</script>

<template>
  <el-descriptions :title="title" :column="columns" border class="app-descriptions">
    <el-descriptions-item
      v-for="item in items"
      :key="item.label"
      :label="item.label"
      :span="item.span"
    >
      <slot name="value" :item="item">
        {{ displayValue(item.value) }}
      </slot>
    </el-descriptions-item>
  </el-descriptions>
</template>

<style scoped>
.app-descriptions :deep(.el-descriptions__label) {
  width: 120px;
  color: var(--text-secondary);
  font-weight: 400;
}

.app-descriptions :deep(.el-descriptions__content) {
  color: var(--text-primary);
}
</style>
