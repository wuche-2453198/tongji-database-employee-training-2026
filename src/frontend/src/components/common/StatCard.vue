<script setup lang="ts">
import type { Component } from 'vue'
import { ArrowRight } from '@element-plus/icons-vue'

withDefaults(
  defineProps<{
    label: string
    value?: string | number | null
    helper?: string
    state?: 'loading' | 'default' | 'empty' | 'link'
    to?: string
    icon?: Component
  }>(),
  {
    value: null,
    helper: '',
    state: 'default',
    to: '',
    icon: undefined,
  },
)
</script>

<template>
  <component
    :is="state === 'link' && to ? 'RouterLink' : 'article'"
    :to="state === 'link' && to ? to : undefined"
    class="stat-card"
    :class="{ 'stat-card--link': state === 'link' }"
  >
    <template v-if="state === 'loading'">
      <span class="sr-only">{{ label }}正在加载</span>
      <el-skeleton :rows="2" animated />
    </template>
    <template v-else>
      <span class="stat-card__head">
        <span v-if="icon" class="stat-card__icon" aria-hidden="true">
          <el-icon><component :is="icon" /></el-icon>
        </span>
        <span class="stat-card__label">{{ label }}</span>
      </span>
      <strong class="stat-card__value numeric">{{ state === 'empty' ? 0 : (value ?? '—') }}</strong>
      <span v-if="helper" class="stat-card__helper">{{ helper }}</span>
      <span v-if="state === 'link'" class="stat-card__link-hint">
        查看详情
        <el-icon aria-hidden="true"><ArrowRight /></el-icon>
      </span>
    </template>
  </component>
</template>

<style scoped>
.stat-card {
  position: relative;
  display: grid;
  min-height: 144px;
  align-content: start;
  gap: var(--space-2);
  padding: var(--space-5);
  border: var(--border-default);
  border-radius: var(--radius-lg);
  color: inherit;
  background: var(--color-surface);
  box-shadow: var(--shadow-sm);
  text-decoration: none;
}

.stat-card--link:hover {
  border-color: var(--color-primary);
}

.stat-card__head {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.stat-card__label {
  color: var(--text-secondary);
  line-height: var(--line-height-body);
}

.stat-card__icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border-radius: var(--radius-md);
  color: var(--color-primary);
  background: var(--color-primary-soft);
  font-size: 16px;
}

.stat-card__value {
  font-size: 32px;
  line-height: 40px;
}

.stat-card__helper,
.stat-card__link-hint {
  color: var(--text-secondary);
  font-size: var(--font-size-caption);
  line-height: var(--line-height-caption);
}

.stat-card__link-hint {
  display: inline-flex;
  align-items: center;
  gap: var(--space-1);
  color: var(--text-link);
}
</style>
