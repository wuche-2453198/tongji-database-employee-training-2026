<script setup lang="ts">
import type { SelectOption } from '@/types/ui'

withDefaults(
  defineProps<{
    modelValue: string | number | null
    options: SelectOption[]
    placeholder?: string
    disabled?: boolean
    clearable?: boolean
    accessibleLabel: string
  }>(),
  {
    placeholder: '请选择',
    disabled: false,
    clearable: true,
  },
)

const emit = defineEmits<{
  'update:modelValue': [value: string | number | null]
  change: [value: string | number | null]
}>()

function handleChange(value: string | number | null) {
  emit('update:modelValue', value)
  emit('change', value)
}
</script>

<template>
  <el-select
    :model-value="modelValue"
    :placeholder="placeholder"
    :disabled="disabled"
    :clearable="clearable"
    :aria-label="accessibleLabel"
    class="app-select"
    @update:model-value="handleChange"
  >
    <el-option
      v-for="option in options"
      :key="option.value"
      :label="option.label"
      :value="option.value"
      :disabled="option.disabled"
    />
  </el-select>
</template>

<style scoped>
.app-select {
  width: 100%;
}
</style>
