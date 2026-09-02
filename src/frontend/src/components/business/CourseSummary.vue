<script setup lang="ts">
import { computed } from 'vue'

import type { CourseInfo, DescriptionItem, StatusSemantic } from '@/types/ui'
import AppDescriptions from '@/components/common/AppDescriptions.vue'
import StatusTag from '@/components/common/StatusTag.vue'

type CourseSummaryState = 'published' | 'closed' | 'full' | 'restricted'

const props = defineProps<{
  course: CourseInfo
  state: CourseSummaryState
  restrictionReason?: string
}>()

const stateMeta = computed<{ label: string; semantic: StatusSemantic; message: string }>(() => {
  const values = {
    published: { label: '已发布', semantic: 'success', message: '当前课程可以提交培训申请。' },
    closed: { label: '已关闭', semantic: 'neutral', message: '课程已关闭，暂时不能申请或报名。' },
    full: { label: '名额已满', semantic: 'warning', message: '课程暂无剩余名额，请选择其他课程。' },
    restricted: {
      label: '资格受限',
      semantic: 'error',
      message: props.restrictionReason || '当前账号不满足申请或报名条件。',
    },
  } as const

  return values[props.state]
})

const items = computed<DescriptionItem[]>(() => [
  { label: '课程类型', value: props.course.type },
  { label: '培训讲师', value: props.course.trainer },
  { label: '培训时间', value: props.course.schedule },
  { label: '培训地点', value: props.course.location },
  { label: '培训学时', value: `${props.course.hours} 学时` },
])
</script>

<template>
  <article class="course-summary">
    <div class="course-summary__top">
      <div class="course-summary__cover">
        <img
          v-if="course.cover"
          :src="course.cover"
          :alt="`${course.name} 封面`"
          loading="lazy"
          decoding="async"
        />
        <span v-else class="course-summary__cover-fallback">{{ course.type }}</span>
      </div>
      <div class="course-summary__main">
        <div class="course-summary__header">
          <div>
            <p class="course-summary__eyebrow">课程摘要</p>
            <h3>{{ course.name }}</h3>
          </div>
          <StatusTag :label="stateMeta.label" :semantic="stateMeta.semantic" />
        </div>
        <AppDescriptions :items="items" :columns="2" />
      </div>
    </div>

    <div class="course-summary__notice" :class="`course-summary__notice--${stateMeta.semantic}`">
      <span>{{ stateMeta.message }}</span>
      <span v-if="state === 'full' && course.remainingSeats != null" class="numeric">
        剩余名额：{{ course.remainingSeats }}
      </span>
    </div>

    <div v-if="$slots.action" class="course-summary__action">
      <slot name="action" :disabled="state !== 'published'" />
    </div>
  </article>
</template>

<style scoped>
.course-summary {
  display: grid;
  gap: var(--space-5);
  padding: var(--space-6);
  border: var(--border-default);
  border-radius: var(--radius-lg);
  background: var(--color-surface);
}

.course-summary__top {
  display: grid;
  grid-template-columns: minmax(220px, 300px) minmax(0, 1fr);
  gap: var(--space-5);
  align-items: start;
}

.course-summary__cover {
  aspect-ratio: 16 / 9;
  overflow: hidden;
  border-radius: var(--radius-md);
  background: var(--color-surface-subtle);
}

.course-summary__cover img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.course-summary__cover-fallback {
  display: grid;
  width: 100%;
  height: 100%;
  place-items: center;
  color: var(--text-on-primary);
  background: linear-gradient(120deg, #1e3a8a, #2563eb);
  font-weight: 600;
}

.course-summary__main {
  display: grid;
  gap: var(--space-4);
}

.course-summary__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-4);
}

.course-summary__eyebrow {
  margin: 0 0 var(--space-1);
  color: var(--text-secondary);
  font-size: var(--font-size-caption);
}

.course-summary h3 {
  margin: 0;
  font-size: var(--font-size-title-section);
  line-height: var(--line-height-title-section);
}

.course-summary__notice {
  display: flex;
  justify-content: space-between;
  gap: var(--space-4);
  padding: var(--space-3) var(--space-4);
  border-radius: var(--radius-md);
  color: var(--color-neutral-text);
  background: var(--color-neutral-soft);
}

.course-summary__notice--success {
  color: var(--color-success-text);
  background: var(--color-success-soft);
}

.course-summary__notice--warning {
  color: var(--color-warning-text);
  background: var(--color-warning-soft);
}

.course-summary__notice--error {
  color: var(--color-error-text);
  background: var(--color-error-soft);
}

.course-summary__action {
  display: flex;
  justify-content: flex-end;
}

@media (width <= 700px) {
  .course-summary__top {
    grid-template-columns: 1fr;
  }
}
</style>
