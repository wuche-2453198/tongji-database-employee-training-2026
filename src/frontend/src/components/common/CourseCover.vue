<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { getCourseCover } from '@/utils/course-cover'

const props = withDefaults(
  defineProps<{
    courseId?: number | null
    courseType: string | null
    /** 有语义时的 alt 文本；装饰图可传空字符串 */
    alt?: string
  }>(),
  { courseId: null, alt: '' },
)

const loadFailed = ref(false)
const src = computed(() => getCourseCover(props.courseId, props.courseType))

watch(
  () => [props.courseId, props.courseType],
  () => {
    loadFailed.value = false
  },
)

function onError() {
  loadFailed.value = true
}

const showFallback = computed(() => loadFailed.value || !src.value)
</script>

<template>
  <div class="course-cover">
    <img
      v-if="!showFallback"
      :src="src"
      :alt="alt"
      loading="lazy"
      class="course-cover__img"
      @error="onError"
    >
    <div v-else class="course-cover__fallback">
      <span class="course-cover__type">{{ courseType || '培训课程' }}</span>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.course-cover {
  position: relative;
  width: 100%;
  aspect-ratio: 16 / 8.5;
  max-height: 180px;
  overflow: hidden;
  background: linear-gradient(135deg, #dbe4f0 0%, #c3d0e4 50%, #aab9d1 100%);
}

.course-cover__img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
  transition: transform 0.3s ease;
}

.course-cover:hover .course-cover__img {
  transform: scale(1.02);
}

.course-cover__fallback {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
}

.course-cover__type {
  font-size: 13px;
  font-weight: 500;
  color: rgba(17, 24, 39, 0.55);
  letter-spacing: 1px;
}
</style>
