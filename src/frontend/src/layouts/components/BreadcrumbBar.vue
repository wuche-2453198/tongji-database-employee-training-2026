<script setup lang="ts">
defineProps<{
  items: string[]
}>()
</script>

<template>
  <nav v-if="items.length" class="breadcrumb-bar" aria-label="面包屑">
    <el-breadcrumb separator="/">
      <el-breadcrumb-item v-for="item in items" :key="item">{{ item }}</el-breadcrumb-item>
    </el-breadcrumb>
  </nav>
</template>

<style scoped>
.breadcrumb-bar {
  display: flex;
  min-height: 40px;
  align-items: center;
  padding: 0 var(--layout-page-padding);
  border-bottom: var(--border-default);
  background: var(--color-surface);
}
</style>
