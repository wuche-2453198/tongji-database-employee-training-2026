<script setup lang="ts">
defineProps<{
  loading: boolean
  error: boolean
  errorMessage?: string
  empty: boolean
  emptyDescription?: string
  emptyImage?: string
}>()

defineEmits<{
  retry: []
}>()

defineSlots<{
  default(): unknown
  'empty-actions'(): unknown
}>()
</script>

<template>
  <div class="async-state" :style="{ minHeight: loading || error || empty ? '240px' : 'auto' }">
    <!-- 加载中 -->
    <div v-if="loading" class="async-state__loading" v-loading="true" element-loading-text="加载中..." />

    <!-- 错误 -->
    <div v-else-if="error" class="async-state__error">
      <el-alert
        :title="errorMessage || '加载失败'"
        type="error"
        show-icon
        :closable="false"
      >
        <template #default>
          <el-button size="small" @click="$emit('retry')">重试</el-button>
        </template>
      </el-alert>
    </div>

    <!-- 空数据 -->
    <div v-else-if="empty" class="async-state__empty">
      <el-empty
        :description="emptyDescription || '暂无数据'"
        :image="emptyImage || 'no-data'"
      >
        <template v-if="$slots['empty-actions']" #default>
          <slot name="empty-actions" />
        </template>
      </el-empty>
    </div>

    <!-- 正常内容 -->
    <slot v-else />
  </div>
</template>

<style lang="scss" scoped>
.async-state {
  position: relative;
}

.async-state__loading {
  position: absolute;
  inset: 0;
  background: var(--color-bg-card);
  border-radius: var(--radius-md);
}

.async-state__error {
  margin-bottom: var(--space-md);
}

.async-state__empty {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 320px;
}
</style>
