<script setup lang="ts">
import AppButton from './AppButton.vue'

withDefaults(
  defineProps<{
    title?: string
    expanded?: boolean
    searching?: boolean
    showHeader?: boolean
    showExpandControl?: boolean
  }>(),
  {
    title: '筛选条件',
    expanded: false,
    searching: false,
    showHeader: true,
    showExpandControl: true,
  },
)

const emit = defineEmits<{
  search: []
  reset: []
  'update:expanded': [value: boolean]
}>()
</script>

<template>
  <section class="search-panel" aria-labelledby="search-panel-title">
    <div v-if="showHeader" class="search-panel__header">
      <h2 id="search-panel-title">{{ title }}</h2>
      <AppButton
        v-if="showExpandControl && $slots.expanded"
        :label="expanded ? '收起条件' : '展开条件'"
        variant="text"
        @click="emit('update:expanded', !expanded)"
      />
    </div>

    <form class="search-panel__form" role="search" @submit.prevent="emit('search')">
      <div class="search-panel__fields">
        <slot />
      </div>
      <div
        v-if="$slots.expanded"
        v-show="expanded"
        class="search-panel__fields search-panel__expanded"
      >
        <slot name="expanded" />
      </div>
      <div class="search-panel__actions">
        <AppButton
          label="查询"
          loading-text="查询中…"
          variant="primary"
          native-type="submit"
          :loading="searching"
        />
        <AppButton label="重置" native-type="button" :disabled="searching" @click="emit('reset')" />
      </div>
    </form>
  </section>
</template>

<style scoped>
.search-panel {
  padding: var(--space-6);
  border: var(--border-default);
  border-radius: var(--radius-lg);
  background: var(--color-surface);
}

.search-panel__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-4);
  margin-bottom: var(--space-4);
}

.search-panel h2 {
  margin: 0;
  font-size: var(--font-size-title-component);
  line-height: var(--line-height-title-component);
}

.search-panel__form {
  display: grid;
  gap: var(--space-4);
}

.search-panel__fields {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: var(--space-4);
}

.search-panel__expanded {
  padding-top: var(--space-4);
  border-top: var(--border-default);
}

.search-panel__actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-2);
}
</style>
