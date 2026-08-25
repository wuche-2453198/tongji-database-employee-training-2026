<script setup lang="ts">
import { computed } from 'vue'

type ButtonVariant = 'primary' | 'default' | 'text' | 'danger'

const props = withDefaults(
  defineProps<{
    label: string
    variant?: ButtonVariant
    loading?: boolean
    loadingText?: string
    disabled?: boolean
    disabledReason?: string
    nativeType?: 'button' | 'submit' | 'reset'
  }>(),
  {
    variant: 'default',
    loading: false,
    loadingText: '处理中…',
    disabled: false,
    disabledReason: '',
    nativeType: 'button',
  },
)

const emit = defineEmits<{
  click: [event: MouseEvent]
}>()

const elementType = computed(() => {
  if (props.variant === 'primary') return 'primary'
  if (props.variant === 'danger') return 'danger'
  return undefined
})
</script>

<template>
  <span class="app-button-wrap">
    <el-button
      :type="elementType"
      :link="variant === 'text'"
      :loading="loading"
      :disabled="disabled || loading"
      :native-type="nativeType"
      @click="emit('click', $event)"
    >
      <slot name="icon" />
      {{ loading ? loadingText : label }}
    </el-button>
    <span v-if="disabled && disabledReason" class="app-button-reason">{{ disabledReason }}</span>
  </span>
</template>

<style scoped>
.app-button-wrap {
  display: inline-flex;
  flex-direction: column;
  align-items: flex-start;
  gap: var(--space-1);
}

.app-button-reason {
  color: var(--text-secondary);
  font-size: var(--font-size-caption);
  line-height: var(--line-height-caption);
}
</style>
