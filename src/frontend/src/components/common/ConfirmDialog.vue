<script setup lang="ts">
import { useId } from 'vue'

import AppButton from './AppButton.vue'

withDefaults(
  defineProps<{
    modelValue: boolean
    title: string
    description: string
    confirmLabel: string
    type?: 'confirm' | 'danger'
    loading?: boolean
    loadingText?: string
  }>(),
  {
    type: 'confirm',
    loading: false,
    loadingText: '处理中…',
  },
)

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  confirm: []
  cancel: []
}>()

const descriptionId = useId()

function close() {
  emit('update:modelValue', false)
  emit('cancel')
}
</script>

<template>
  <el-dialog
    :model-value="modelValue"
    :title="title"
    width="480px"
    :aria-describedby="descriptionId"
    :close-on-click-modal="!loading"
    :close-on-press-escape="!loading"
    :show-close="!loading"
    destroy-on-close
    @update:model-value="emit('update:modelValue', $event)"
  >
    <div
      class="confirm-dialog__body"
      :class="{ 'confirm-dialog__body--danger': type === 'danger' }"
    >
      <p :id="descriptionId">{{ description }}</p>
      <slot />
    </div>

    <template #footer>
      <div class="confirm-dialog__actions">
        <AppButton label="取消" :disabled="loading" @click="close" />
        <AppButton
          :label="confirmLabel"
          :variant="type === 'danger' ? 'danger' : 'primary'"
          :loading="loading"
          :loading-text="loadingText"
          @click="emit('confirm')"
        />
      </div>
    </template>
  </el-dialog>
</template>

<style scoped>
.confirm-dialog__body {
  padding: var(--space-3) var(--space-4);
  border-left: 3px solid var(--color-primary);
  border-radius: var(--radius-sm);
  background: var(--color-primary-soft);
}

.confirm-dialog__body--danger {
  border-left-color: var(--color-error);
  background: var(--color-error-soft);
}

.confirm-dialog__body p {
  margin: 0;
  color: var(--text-primary);
  line-height: var(--line-height-body);
}

.confirm-dialog__actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-2);
}
</style>
