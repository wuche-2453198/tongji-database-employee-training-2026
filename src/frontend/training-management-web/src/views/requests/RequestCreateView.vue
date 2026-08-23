<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { LatestRequestController, SingleFlightController } from '@/api/request-control'
import AppButton from '@/components/common/AppButton.vue'
import CourseSummary from '@/components/business/CourseSummary.vue'
import FormField from '@/components/common/FormField.vue'
import PageHeader from '@/components/common/PageHeader.vue'
import PageState from '@/components/common/PageState.vue'
import { getCourseService } from '@/services/course'
import { getTrainingRequestService } from '@/services/training-request'
import { useAuthStore } from '@/stores/auth'
import { isServiceError, type UiError } from '@/types/api'
import type { CourseDetail } from '@/domains/course'
import type { CourseInfo } from '@/types/ui'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const latestQuery = new LatestRequestController()
const submissions = new SingleFlightController()
const course = ref<CourseDetail | null>(null)
const loading = ref(false)
const submitting = ref(false)
const error = ref<UiError | null>(null)
const submissionError = ref<UiError | null>(null)
const reasonError = ref('')
const reasonInput = ref<HTMLTextAreaElement | null>(null)
const form = reactive({ reason: '' })
const courseId = computed(() =>
  typeof route.query.courseId === 'string' ? route.query.courseId : '',
)
const currentUser = computed(() => authStore.currentUser)
const courseInfo = computed<CourseInfo | null>(() =>
  course.value
    ? {
        name: course.value.name,
        type: course.value.typeLabel,
        trainer: course.value.trainerName,
        schedule: `${formatDateTime(course.value.startTime)} - ${formatDateTime(course.value.endTime)}`,
        location: course.value.location,
        hours: course.value.hours ?? 0,
        remainingSeats: course.value.remainingSeats,
      }
    : null,
)
const summaryState = computed<'published' | 'closed' | 'full' | 'restricted'>(() => {
  if (!course.value || course.value.status === 'CLOSED') return 'closed'
  if (course.value.remainingSeats <= 0) return 'full'
  if (!course.value.eligibility.apply.allowed) return 'restricted'
  return 'published'
})

function formatDateTime(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '—'
  return new Intl.DateTimeFormat('zh-CN', { dateStyle: 'medium', timeStyle: 'short' }).format(date)
}

async function loadCourse(): Promise<void> {
  if (!courseId.value) {
    error.value = {
      kind: 'not-found',
      code: 'INVALID_COURSE_ID',
      message: '缺少课程编号，无法发起申请。',
      fieldErrors: [],
      retryable: false,
      resultUnknown: false,
    }
    return
  }
  loading.value = true
  error.value = null
  try {
    const service = await getCourseService()
    course.value = await latestQuery.run((signal) => service.getCourse(courseId.value, { signal }))
  } catch (caught) {
    if (isServiceError(caught) && caught.ui.kind === 'canceled') return
    course.value = null
    error.value = isServiceError(caught)
      ? caught.ui
      : {
          kind: 'unknown',
          code: 'UNKNOWN',
          message: '课程信息加载失败。',
          fieldErrors: [],
          retryable: false,
          resultUnknown: false,
        }
  } finally {
    loading.value = false
  }
}

async function focusReason(): Promise<void> {
  await nextTick()
  reasonInput.value?.focus()
}

async function submit(): Promise<void> {
  reasonError.value = ''
  submissionError.value = null
  const reason = form.reason.trim()
  if (!reason) {
    reasonError.value = '请填写申请理由。'
    await focusReason()
    return
  }
  if (reason.length > 500) {
    reasonError.value = '申请理由不能超过500字。'
    await focusReason()
    return
  }
  if (!course.value) return
  submitting.value = true
  error.value = null
  try {
    const service = await getTrainingRequestService()
    const request = await submissions.run(`create:${courseId.value}`, () =>
      service.create({ courseId: courseId.value, reason }),
    )
    await router.push({ name: 'my-request-list', query: { submitted: request.id } })
  } catch (caught) {
    if (isServiceError(caught) && caught.ui.resultUnknown) {
      try {
        const service = await getTrainingRequestService()
        const finalState = await service.listMine({ page: 1, pageSize: 50 })
        const confirmed = finalState.items.find(
          (item) => item.courseId === courseId.value && item.status !== 'DEPT_REJECTED',
        )
        if (confirmed) {
          await router.push({ name: 'my-request-list', query: { submitted: confirmed.id } })
          return
        }
      } catch {
        // Keep the original unknown-result message when the follow-up query also fails.
      }
    }
    submissionError.value = isServiceError(caught)
      ? caught.ui
      : {
          kind: 'unknown',
          code: 'UNKNOWN',
          message: '提交失败，请稍后重试。',
          fieldErrors: [],
          retryable: false,
          resultUnknown: false,
        }
  } finally {
    submitting.value = false
  }
}

function goBack(): void {
  if (window.history.length > 1) router.back()
  else void router.replace({ name: 'course-detail', params: { id: courseId.value } })
}

onMounted(loadCourse)
onBeforeUnmount(() => latestQuery.cancel())
</script>

<template>
  <section class="request-create-view">
    <PageHeader
      title="发起培训申请"
      description="提交申请理由后等待部门主管审批。"
      context="detail"
      :breadcrumbs="['我的培训', '我的申请', '发起申请']"
      @back="goBack"
    />
    <PageState v-if="loading" state="loading" />
    <PageState
      v-else-if="error?.kind === 'forbidden'"
      state="forbidden"
      :description="error.message"
      secondary-label="返回课程详情"
      compact
      @secondary="goBack"
    />
    <PageState
      v-else-if="error?.kind === 'not-found'"
      state="not-found"
      :description="error.message"
      secondary-label="返回课程详情"
      compact
      @secondary="goBack"
    />
    <PageState
      v-else-if="error"
      state="error"
      :description="error.message"
      primary-label="重新加载课程"
      :trace-id="error.traceId"
      @primary="loadCourse"
    />
    <template v-else-if="course && courseInfo">
      <CourseSummary
        :course="courseInfo"
        :state="summaryState"
        :restriction-reason="course.eligibility.apply.reason"
      />
      <el-form class="request-create-view__form" label-position="top" @submit.prevent="submit">
        <div class="request-create-view__context">
          <h2>申请信息</h2>
          <div class="request-create-view__context-grid">
            <FormField
              label="申请人"
              readonly
              :display-value="currentUser?.displayName || '当前用户'"
            />
            <FormField
              label="所属部门"
              readonly
              :display-value="currentUser?.departmentName || '—'"
            />
          </div>
          <FormField
            label="申请理由"
            required
            :error="reasonError"
            helper="请说明参加本课程的工作需要或学习目标，最多500字。"
          >
            <el-input
              ref="reasonInput"
              v-model="form.reason"
              type="textarea"
              :rows="6"
              maxlength="500"
              show-word-limit
              placeholder="请输入申请理由"
              aria-label="申请理由"
            />
          </FormField>
        </div>
        <div v-if="submissionError" class="request-create-view__feedback" role="alert">
          <strong>{{ submissionError.message }}</strong>
          <span v-if="submissionError.code === 'REQUEST_EXISTS'"
            >请进入“我的申请”查看当前记录。</span
          >
          <span v-else-if="submissionError.resultUnknown"
            >提交结果未知，请查询最终状态后再决定下一步。</span
          >
        </div>
        <div class="request-create-view__actions">
          <AppButton label="取消" :disabled="submitting" @click="goBack" />
          <AppButton
            label="提交申请"
            loading-text="提交中…"
            variant="primary"
            :loading="submitting"
            :disabled="summaryState !== 'published'"
            @click="submit"
          />
        </div>
      </el-form>
    </template>
  </section>
</template>

<style scoped>
.request-create-view {
  display: grid;
  gap: var(--space-6);
}
.request-create-view__form {
  display: grid;
  gap: var(--space-5);
  padding: var(--space-6);
  border: var(--border-default);
  border-radius: var(--radius-lg);
  background: var(--color-surface);
}
.request-create-view__context {
  display: grid;
  gap: var(--space-4);
}
.request-create-view__context h2 {
  margin: 0;
  font-size: var(--font-size-title-component);
}
.request-create-view__context-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: var(--space-4);
}
.request-create-view__feedback {
  display: grid;
  gap: var(--space-1);
  padding: var(--space-3) var(--space-4);
  border-radius: var(--radius-md);
  color: var(--color-error-text);
  background: var(--color-error-soft);
}
.request-create-view__actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-3);
  padding-top: var(--space-2);
  border-top: var(--border-default);
}
@media (width <= 700px) {
  .request-create-view__context-grid {
    grid-template-columns: 1fr;
  }
}
</style>
