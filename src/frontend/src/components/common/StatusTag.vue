<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  type: string
  labelMap?: Record<string, string>
}>()

const label = computed(() => {
  if (props.labelMap && props.type in props.labelMap) {
    return props.labelMap[props.type]
  }
  return props.type
})

const tagType = computed(() => {
  const t = props.type.toUpperCase()
  switch (t) {
    // 成功态 — 绿色
    case 'PUBLISHED':
    case 'COMPLETED':
    case 'SIGNED_IN':
    case 'ACTIVE':
      return 'success'
    // 待处理 — 橙色
    case 'PENDING':
    case 'REGISTERED':
      return 'warning'
    // 进行中 — 蓝色
    case 'DEPT_APPROVED':
    case 'HR_FILED':
      return 'primary'
    // 驳回/失败 — 红色
    case 'DEPT_REJECTED':
    case 'ABSENT':
      return 'danger'
    // 关闭/取消/离职/解除/草稿 — 灰色
    case 'DRAFT':
    case 'CLOSED':
    case 'CANCELED':
    case 'RESIGNED':
    case 'RELEASED':
      return 'info'
    default:
      return 'info'
  }
})
</script>

<template>
  <el-tag :type="tagType" size="small" effect="light">
    {{ label }}
  </el-tag>
</template>
