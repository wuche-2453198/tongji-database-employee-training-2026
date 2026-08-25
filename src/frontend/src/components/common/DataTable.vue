<script setup lang="ts">
import type { TableColumn } from '@/types/ui'

import PageState from './PageState.vue'

withDefaults(
  defineProps<{
    columns: TableColumn[]
    rows?: Record<string, unknown>[]
    rowKey?: string
    state?: 'default' | 'loading' | 'empty' | 'error'
    emptyTitle?: string
    emptyDescription?: string
    traceId?: string
    hasActions?: boolean
  }>(),
  {
    rows: () => [],
    rowKey: 'id',
    state: 'default',
    emptyTitle: '暂无数据',
    emptyDescription: '当前还没有可展示的记录。',
    traceId: '',
    hasActions: false,
  },
)

const emit = defineEmits<{
  retry: []
}>()

function formatCell(value: unknown): string {
  if (value === null || value === undefined || value === '') return '—'
  return String(value)
}
</script>

<template>
  <section class="data-table" :aria-busy="state === 'loading'">
    <el-table
      v-if="state === 'default' || state === 'loading'"
      v-loading="state === 'loading'"
      :data="rows"
      :row-key="rowKey"
      table-layout="fixed"
      empty-text="暂无数据"
    >
      <el-table-column
        v-for="column in columns"
        :key="column.key"
        :prop="column.key"
        :label="column.label"
        :width="column.width"
        :min-width="column.minWidth"
        :align="column.align || 'left'"
        show-overflow-tooltip
      >
        <template #default="scope">
          <slot name="cell" :column="column" :row="scope.row" :value="scope.row[column.key]">
            {{ formatCell(scope.row[column.key]) }}
          </slot>
        </template>
      </el-table-column>

      <el-table-column v-if="hasActions" label="操作" width="160" fixed="right">
        <template #default="scope">
          <div class="data-table__actions">
            <slot name="actions" :row="scope.row" />
          </div>
        </template>
      </el-table-column>
    </el-table>

    <PageState
      v-else-if="state === 'empty'"
      state="empty"
      compact
      :title="emptyTitle"
      :description="emptyDescription"
    />

    <PageState v-else state="error" compact :trace-id="traceId" @primary="emit('retry')" />
  </section>
</template>

<style scoped>
.data-table {
  overflow: hidden;
  border: var(--border-default);
  border-radius: var(--radius-lg);
  background: var(--color-surface);
}

.data-table__actions {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.data-table :deep(.page-state) {
  border: 0;
  border-radius: 0;
}
</style>
