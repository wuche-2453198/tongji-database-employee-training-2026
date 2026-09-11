<script setup lang="ts">
import AppButton from './AppButton.vue'

withDefaults(
  defineProps<{
    title: string
    description?: string
    context?: 'list' | 'detail'
    breadcrumbs?: string[]
    actionLabel?: string
  }>(),
  {
    description: '',
    context: 'list',
    breadcrumbs: () => [],
    actionLabel: '',
  },
)

const emit = defineEmits<{
  back: []
  action: []
}>()
</script>

<template>
  <header class="page-header">
    <nav v-if="breadcrumbs.length" aria-label="面包屑" class="page-header__breadcrumbs">
      <ol>
        <li v-for="item in breadcrumbs" :key="item">{{ item }}</li>
      </ol>
    </nav>

    <div class="page-header__body">
      <div class="page-header__copy">
        <AppButton
          v-if="context === 'detail'"
          label="返回列表"
          variant="text"
          @click="emit('back')"
        />
        <h1>{{ title }}</h1>
        <p v-if="description">{{ description }}</p>
      </div>

      <div v-if="$slots.action || actionLabel" class="page-header__action">
        <slot name="action">
          <AppButton
            v-if="actionLabel"
            :label="actionLabel"
            variant="primary"
            @click="emit('action')"
          />
        </slot>
      </div>
    </div>
  </header>
</template>

<style scoped>
.page-header {
  display: grid;
  gap: var(--space-3);
}

.page-header__breadcrumbs ol {
  display: flex;
  gap: var(--space-2);
  padding: 0;
  margin: 0;
  color: var(--text-secondary);
  font-size: var(--font-size-helper);
  list-style: none;
}

.page-header__breadcrumbs li:not(:last-child)::after {
  margin-left: var(--space-2);
  color: var(--text-disabled);
  content: '/';
}

.page-header__body {
  display: flex;
  min-height: 64px;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-6);
}

.page-header__copy {
  min-width: 0;
}

.page-header h1 {
  margin: 0;
  color: var(--text-primary);
  font-size: var(--font-size-title-page);
  font-weight: 600;
  line-height: var(--line-height-title-page);
}

.page-header p {
  margin: var(--space-2) 0 0;
  color: var(--text-secondary);
  line-height: var(--line-height-body);
}

.page-header__action {
  flex: 0 0 auto;
}
</style>
