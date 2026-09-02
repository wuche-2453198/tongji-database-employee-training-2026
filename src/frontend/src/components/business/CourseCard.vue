<script setup lang="ts">
import { computed } from 'vue'

import type { CourseStatus, CourseSummary } from '@/domains/course'
import StatusTag from '@/components/common/StatusTag.vue'
import { resolveCourseCover } from '@/utils/course-cover'

const props = defineProps<{ course: CourseSummary }>()

const emit = defineEmits<{ open: [course: CourseSummary] }>()

const cover = computed(() => resolveCourseCover(props.course.type))

const scheduleLabel = computed(() => {
  const start = formatDateTime(props.course.startTime)
  const end = formatDateTime(props.course.endTime)
  return start === '—' && end === '—' ? '时间待定' : `${start} - ${end}`
})

const capacityLabel = computed(() => {
  if (props.course.remainingSeats === null || props.course.registeredCount === null)
    return '名额数据暂不可用'
  return `剩余 ${props.course.remainingSeats} / ${props.course.maxStudents}`
})

function formatDateTime(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '—'
  return new Intl.DateTimeFormat('zh-CN', {
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).format(date)
}

function statusSemantic(status: CourseStatus): 'success' | 'warning' | 'error' | 'neutral' {
  if (status === 'PUBLISHED') return 'success'
  if (status === 'DRAFT') return 'warning'
  if (status === 'CLOSED') return 'neutral'
  return 'error'
}
</script>

<template>
  <button type="button" class="course-card" @click="emit('open', course)">
    <span class="course-card__cover">
      <img v-if="cover" :src="cover" :alt="`${course.name} 封面`" loading="lazy" decoding="async" />
      <span v-else class="course-card__cover-fallback">{{ course.typeLabel }}</span>
      <span class="course-card__type">{{ course.typeLabel }}</span>
    </span>
    <span class="course-card__body">
      <span class="course-card__title-row">
        <span class="course-card__title">{{ course.name }}</span>
        <StatusTag :label="course.statusLabel" :semantic="statusSemantic(course.status)" />
      </span>
      <span class="course-card__meta">
        <span class="course-card__meta-item">
          <span class="course-card__meta-label">讲师</span>
          <span class="course-card__meta-value">{{ course.trainerName }}</span>
        </span>
        <span class="course-card__meta-item">
          <span class="course-card__meta-label">时间</span>
          <span class="course-card__meta-value">{{ scheduleLabel }}</span>
        </span>
        <span class="course-card__meta-item">
          <span class="course-card__meta-label">地点</span>
          <span class="course-card__meta-value">{{ course.location }}</span>
        </span>
      </span>
      <span class="course-card__foot">
        <span>{{ capacityLabel }}</span>
        <span class="course-card__more">查看详情 →</span>
      </span>
    </span>
  </button>
</template>

<style scoped>
.course-card {
  display: flex;
  flex-direction: column;
  overflow: hidden;
  padding: 0;
  border: var(--border-default);
  border-radius: var(--radius-lg);
  color: inherit;
  background: var(--color-surface);
  box-shadow: var(--shadow-sm);
  cursor: pointer;
  text-align: left;
  transition:
    border-color 0.15s ease,
    box-shadow 0.15s ease;
}

.course-card:hover {
  border-color: var(--color-primary);
  box-shadow: var(--shadow-md);
}

.course-card__cover {
  position: relative;
  display: block;
  aspect-ratio: 16 / 9;
  overflow: hidden;
  background: var(--color-surface-subtle);
}

.course-card__cover img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.course-card__cover-fallback {
  display: grid;
  width: 100%;
  height: 100%;
  place-items: center;
  color: var(--text-on-primary);
  background: linear-gradient(120deg, #1e3a8a, #2563eb);
  font-size: var(--font-size-title-component);
  font-weight: 600;
}

.course-card__type {
  position: absolute;
  left: var(--space-3);
  bottom: var(--space-3);
  padding: 2px var(--space-2);
  border-radius: var(--radius-sm);
  color: var(--text-on-primary);
  background: rgb(15 23 42 / 68%);
  font-size: var(--font-size-caption);
}

.course-card__body {
  display: grid;
  gap: var(--space-4);
  padding: var(--space-4);
}

.course-card__title-row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-3);
}

.course-card__title {
  color: var(--text-primary);
  font-size: var(--font-size-title-component);
  font-weight: 600;
  line-height: var(--line-height-title-component);
}

.course-card__meta {
  display: grid;
  gap: var(--space-2);
}

.course-card__meta-item {
  display: flex;
  gap: var(--space-3);
}

.course-card__meta-label {
  flex: 0 0 40px;
  color: var(--text-secondary);
  font-size: var(--font-size-caption);
}

.course-card__meta-value {
  color: var(--text-secondary);
  line-height: var(--line-height-helper);
}

.course-card__foot {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
  padding-top: var(--space-3);
  border-top: var(--border-default);
  color: var(--text-secondary);
  font-size: var(--font-size-caption);
}

.course-card__more {
  color: var(--text-link);
}
</style>
