<script setup lang="ts">
defineProps<{
  title: string
  description?: string
}>()
</script>

<template>
  <div class="page-header">
    <div class="page-header__text">
      <h2>{{ title }}</h2>
      <p v-if="description" class="page-header__desc">{{ description }}</p>
    </div>
    <div v-if="$slots.actions" class="page-header__actions">
      <slot name="actions" />
    </div>
  </div>
</template>

<style lang="scss" scoped>
.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  margin-bottom: var(--space-lg);
  gap: var(--space-md);

  &__text {
    min-width: 0;
  }

  h2 {
    font-size: 20px;
    font-weight: 600;
    color: var(--color-text-primary);
    line-height: 1.4;
  }

  &__desc {
    margin-top: var(--space-xs);
    font-size: 13px;
    color: var(--color-text-secondary);
  }

  &__actions {
    flex-shrink: 0;
    display: flex;
    align-items: center;
    gap: var(--space-sm);
  }
}
</style>
