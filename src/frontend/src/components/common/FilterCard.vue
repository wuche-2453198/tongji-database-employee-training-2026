<script setup lang="ts">
import { ref } from 'vue'

defineProps<{
  /** 展开区域中已生效的筛选项数量，用于折叠时提示 */
  activeCount?: number
}>()

defineEmits<{
  search: []
  reset: []
}>()

defineSlots<{
  default(): unknown
  extra(): unknown
}>()

const expanded = ref(false)
</script>

<template>
  <el-card shadow="never" class="filter-card">
    <el-form inline @submit.prevent="$emit('search')">
      <slot />
      <el-form-item class="filter-card__actions">
        <el-button type="primary" @click="$emit('search')">查询</el-button>
        <el-button @click="$emit('reset')">重置</el-button>
        <el-button
          v-if="$slots.extra"
          text
          type="primary"
          class="filter-card__toggle"
          @click="expanded = !expanded"
        >
          {{ expanded ? '收起筛选' : '展开筛选' }}
          <el-badge
            v-if="!expanded && (activeCount ?? 0) > 0"
            :value="activeCount"
            :offset="[4, -2]"
          />
          <el-icon class="filter-card__toggle-icon">
            <component :is="expanded ? 'ArrowUp' : 'ArrowDown'" />
          </el-icon>
        </el-button>
      </el-form-item>
    </el-form>

    <div v-if="expanded" class="filter-card__extra">
      <slot name="extra" />
    </div>
  </el-card>
</template>

<style lang="scss" scoped>
.filter-card {
  margin-bottom: var(--space-lg);

  :deep(.el-card__body) {
    padding: 16px 20px 4px;
  }
}

.filter-card__actions {
  :deep(.el-form-item__content) {
    gap: var(--space-sm);
  }
}

.filter-card__toggle {
  padding: 0 4px;
}

.filter-card__toggle-icon {
  margin-left: 2px;
}

.filter-card__extra {
  padding-top: 4px;
  border-top: 1px dashed var(--color-divider);
  margin-top: 4px;
}
</style>
