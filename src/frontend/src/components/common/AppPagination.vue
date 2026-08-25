<script setup lang="ts">
withDefaults(
  defineProps<{
    page: number
    pageSize: number
    total: number
    disabled?: boolean
    pageSizes?: number[]
  }>(),
  {
    disabled: false,
    pageSizes: () => [10, 20, 50],
  },
)

const emit = defineEmits<{
  'update:page': [value: number]
  'update:pageSize': [value: number]
  change: []
}>()

function handlePageChange(value: number) {
  emit('update:page', value)
  emit('change')
}

function handleSizeChange(value: number) {
  emit('update:pageSize', value)
  emit('update:page', 1)
  emit('change')
}
</script>

<template>
  <nav class="app-pagination" aria-label="列表分页">
    <el-pagination
      background
      :current-page="page"
      :page-size="pageSize"
      :page-sizes="pageSizes"
      :total="total"
      :disabled="disabled"
      layout="total, sizes, prev, pager, next"
      @current-change="handlePageChange"
      @size-change="handleSizeChange"
    />
  </nav>
</template>

<style scoped>
.app-pagination {
  display: flex;
  justify-content: flex-end;
  margin-top: var(--space-4);
}
</style>
