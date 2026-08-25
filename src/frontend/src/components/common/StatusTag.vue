<script setup lang="ts">
import { computed } from 'vue'

type StatusSemantic = 'success' | 'warning' | 'error' | 'info' | 'neutral'

const props = withDefaults(
  defineProps<{
    label?: string
    semantic?: StatusSemantic
    showDot?: boolean
  }>(),
  {
    label: '未知状态',
    semantic: 'neutral',
    showDot: true,
  },
)

const accessibleLabel = computed(() => `状态：${props.label || '未知状态'}`)
</script>

<template>
  <span
    class="status-tag"
    :class="`status-tag--${semantic}`"
    role="status"
    :aria-label="accessibleLabel"
  >
    <span v-if="showDot" class="status-tag__dot" aria-hidden="true" />
    <span>{{ label || '未知状态' }}</span>
  </span>
</template>

<style scoped>
.status-tag {
  display: inline-flex;
  min-height: 24px;
  align-items: center;
  gap: var(--space-1);
  padding: 2px var(--space-2);
  border: 1px solid currentcolor;
  border-radius: var(--radius-sm);
  font-size: var(--font-size-caption);
  font-weight: 500;
  line-height: var(--line-height-caption);
  white-space: nowrap;
}

.status-tag__dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: currentcolor;
}

.status-tag--success {
  color: var(--color-success-text);
  background: var(--color-success-soft);
}

.status-tag--warning {
  color: var(--color-warning-text);
  background: var(--color-warning-soft);
}

.status-tag--error {
  color: var(--color-error-text);
  background: var(--color-error-soft);
}

.status-tag--info {
  color: var(--color-info-text);
  background: var(--color-info-soft);
}

.status-tag--neutral {
  color: var(--color-neutral-text);
  background: var(--color-neutral-soft);
}
</style>
