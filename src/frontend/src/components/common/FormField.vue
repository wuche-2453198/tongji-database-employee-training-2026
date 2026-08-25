<script setup lang="ts">
withDefaults(
  defineProps<{
    label: string
    prop?: string
    required?: boolean
    error?: string
    helper?: string
    readonly?: boolean
    displayValue?: string | number | null
  }>(),
  {
    prop: '',
    required: false,
    error: '',
    helper: '',
    readonly: false,
    displayValue: null,
  },
)
</script>

<template>
  <el-form-item :label="label" :prop="prop || undefined" :required="required" :error="error">
    <span v-if="readonly" class="form-field__readonly">
      {{
        displayValue === null || displayValue === undefined || displayValue === ''
          ? '—'
          : displayValue
      }}
    </span>
    <slot v-else />
    <p v-if="helper && !error" class="form-field__helper">{{ helper }}</p>
  </el-form-item>
</template>

<style scoped>
.form-field__readonly {
  min-height: 32px;
  color: var(--text-primary);
  line-height: 32px;
}

.form-field__helper {
  width: 100%;
  margin: var(--space-1) 0 0;
  color: var(--text-secondary);
  font-size: var(--font-size-caption);
  line-height: var(--line-height-caption);
}
</style>
