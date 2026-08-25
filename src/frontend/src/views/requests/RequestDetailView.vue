<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { LatestRequestController } from '@/api/request-control'
import AppButton from '@/components/common/AppButton.vue'
import AppDescriptions from '@/components/common/AppDescriptions.vue'
import PageHeader from '@/components/common/PageHeader.vue'
import PageState from '@/components/common/PageState.vue'
import ProcessTimeline from '@/components/common/ProcessTimeline.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import { getCourseService } from '@/services/course'
import { getTrainingRequestService } from '@/services/training-request'
import { useAuthStore } from '@/stores/auth'
import { isServiceError, type UiError } from '@/types/api'
import type { CourseDetail } from '@/domains/course'
import type { TrainingRequest, TrainingRequestStatus } from '@/domains/training-request'
import type { TimelineNode } from '@/types/ui'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const latestQuery = new LatestRequestController()
const request = ref<TrainingRequest | null>(null)
const course = ref<CourseDetail | null>(null)
const loading = ref(false)
const error = ref<UiError | null>(null)
const requestId = computed(() => (typeof route.params.id === 'string' ? route.params.id : ''))
const status = computed<TrainingRequestStatus>(() => request.value?.status ?? 'UNKNOWN')

function formatDateTime(value: string | null): string {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '—'
  return new Intl.DateTimeFormat('zh-CN', { dateStyle: 'medium', timeStyle: 'short' }).format(date)
}

function statusMeta(value: TrainingRequestStatus): {
  label: string
  semantic: 'success' | 'warning' | 'error' | 'info' | 'neutral'
} {
  return ({
    PENDING: { label: '待审批', semantic: 'warning' },
    DEPT_APPROVED: { label: '部门已通过', semantic: 'info' },
    DEPT_REJECTED: { label: '部门已驳回', semantic: 'error' },
    HR_FILED: { label: '已备案', semantic: 'success' },
    UNKNOWN: { label: '未知状态', semantic: 'neutral' },
  }[value] ?? { label: '未知状态', semantic: 'neutral' }) as {
    label: string
    semantic: 'success' | 'warning' | 'error' | 'info' | 'neutral'
  }
}

const timeline = computed<TimelineNode[]>(() => {
  const current = request.value
  const submitted: TimelineNode = {
    title: '员工提交申请',
    meta: current ? formatDateTime(current.submittedAt) : '',
    summary: '申请已提交，等待后续处理。',
    state: 'complete',
  }
  if (!current)
    return [
      submitted,
      { title: '部门主管审批', state: 'future' },
      { title: 'HR 备案', state: 'future' },
    ]
  const deptState =
    current.status === 'PENDING'
      ? 'current'
      : current.status === 'DEPT_REJECTED'
        ? 'error'
        : 'complete'
  const dept: TimelineNode = {
    title: '部门主管审批',
    meta: current.status === 'PENDING' ? '等待处理' : formatDateTime(current.updatedAt),
    summary:
      current.status === 'DEPT_REJECTED'
        ? '部门主管已驳回该申请。'
        : current.status === 'PENDING'
          ? '请等待部门主管处理。'
          : '部门主管已通过该申请。',
    state: deptState,
  }
  const filing: TimelineNode = {
    title: 'HR 备案',
    meta: current.status === 'HR_FILED' ? formatDateTime(current.updatedAt) : '',
    summary:
      current.status === 'HR_FILED'
        ? '申请已完成 HR 备案。'
        : current.status === 'DEPT_REJECTED'
          ? '申请已结束，无需继续备案。'
          : '等待部门审批通过后进入备案。',
    state: current.status === 'HR_FILED' ? 'complete' : 'future',
  }
  return [submitted, dept, filing]
})

async function loadRequest(): Promise<void> {
  if (!requestId.value) {
    error.value = {
      kind: 'not-found',
      code: 'INVALID_REQUEST_ID',
      message: '申请编号无效。',
      fieldErrors: [],
      retryable: false,
      resultUnknown: false,
    }
    return
  }
  loading.value = true
  error.value = null
  try {
    const [requestService, courseService] = await Promise.all([
      getTrainingRequestService(),
      getCourseService(),
    ])
    const result = await latestQuery.run((signal) =>
      requestService.getById(requestId.value, { signal }),
    )
    request.value = result
    try {
      course.value = await courseService.getCourse(result.courseId)
    } catch {
      course.value = null
    }
  } catch (caught) {
    if (isServiceError(caught) && caught.ui.kind === 'canceled') return
    request.value = null
    course.value = null
    error.value = isServiceError(caught)
      ? caught.ui
      : {
          kind: 'unknown',
          code: 'UNKNOWN',
          message: '申请详情加载失败。',
          fieldErrors: [],
          retryable: false,
          resultUnknown: false,
        }
  } finally {
    loading.value = false
  }
}

function goBack(): void {
  if (window.history.length > 1) {
    router.back()
    return
  }
  const role = authStore.currentUser?.primaryRole
  void router.replace({
    name:
      role === 'DEPT_MANAGER'
        ? 'department-approval'
        : role === 'HR'
          ? 'hr-filing'
          : 'my-request-list',
  })
}

onMounted(loadRequest)
onBeforeUnmount(() => latestQuery.cancel())
</script>

<template>
  <section class="request-detail-view">
    <PageHeader
      title="申请详情"
      context="detail"
      :breadcrumbs="['我的培训', '申请详情']"
      @back="goBack"
    >
      <template #action
        ><AppButton label="刷新状态" :loading="loading" @click="loadRequest"
      /></template>
    </PageHeader>
    <PageState v-if="loading" state="loading" />
    <PageState
      v-else-if="error?.kind === 'forbidden'"
      state="forbidden"
      :description="error.message"
      secondary-label="返回上一页"
      compact
      @secondary="goBack"
    />
    <PageState
      v-else-if="error?.kind === 'not-found'"
      state="not-found"
      :description="error.message"
      secondary-label="返回上一页"
      compact
      @secondary="goBack"
    />
    <PageState
      v-else-if="error"
      state="error"
      :description="error.message"
      :trace-id="error.traceId"
      primary-label="重新加载"
      @primary="loadRequest"
    />
    <template v-else-if="request">
      <section class="request-detail-view__title-row">
        <div>
          <p class="request-detail-view__eyebrow">培训申请 {{ request.id }}</p>
          <h2>{{ request.courseName }}</h2>
        </div>
        <StatusTag :label="statusMeta(status).label" :semantic="statusMeta(status).semantic" />
      </section>
      <section class="request-detail-view__panel">
        <h2>申请流程</h2>
        <ProcessTimeline :nodes="timeline" accessible-label="培训申请处理流程" />
      </section>
      <section class="request-detail-view__grid">
        <article class="request-detail-view__panel">
          <h2>申请信息</h2>
          <AppDescriptions
            :items="[
              { label: '申请编号', value: request.id },
              { label: '申请状态', value: statusMeta(status).label },
              { label: '申请人', value: request.employeeName },
              { label: '所属部门', value: request.departmentName },
              { label: '提交时间', value: formatDateTime(request.submittedAt) },
              { label: '更新时间', value: formatDateTime(request.updatedAt) },
              { label: '申请理由', value: request.reason, span: 2 },
            ]"
          />
        </article>
        <aside class="request-detail-view__panel">
          <h2>课程摘要</h2>
          <AppDescriptions
            v-if="course"
            :columns="1"
            :items="[
              { label: '课程编号', value: course.id },
              { label: '课程类型', value: course.typeLabel },
              { label: '培训讲师', value: course.trainerName },
              {
                label: '培训时间',
                value: `${formatDateTime(course.startTime)} - ${formatDateTime(course.endTime)}`,
              },
              { label: '培训地点', value: course.location },
              { label: '剩余名额', value: `${course.remainingSeats} / ${course.maxStudents}` },
            ]"
          />
          <p v-else class="request-detail-view__muted">课程摘要暂时不可用，请稍后刷新。</p>
        </aside>
      </section>
      <p class="request-detail-view__security-note">
        页面展示范围由服务端按当前角色和数据范围校验；当前页面不接受员工编号或部门编号作为可信参数。
      </p>
    </template>
  </section>
</template>

<style scoped>
.request-detail-view {
  display: grid;
  gap: var(--space-6);
}
.request-detail-view__title-row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-4);
  padding: var(--space-6);
  border: var(--border-default);
  border-radius: var(--radius-lg);
  background: var(--color-surface);
}
.request-detail-view__eyebrow {
  margin: 0 0 var(--space-1);
  color: var(--text-secondary);
  font-size: var(--font-size-caption);
}
.request-detail-view h2 {
  margin: 0;
  font-size: var(--font-size-title-component);
  line-height: var(--line-height-title-component);
}
.request-detail-view__title-row h2 {
  font-size: var(--font-size-title-section);
}
.request-detail-view__panel {
  display: grid;
  gap: var(--space-5);
  padding: var(--space-6);
  border: var(--border-default);
  border-radius: var(--radius-lg);
  background: var(--color-surface);
}
.request-detail-view__grid {
  display: grid;
  grid-template-columns: minmax(0, 1.35fr) minmax(300px, 0.65fr);
  gap: var(--space-6);
}
.request-detail-view__muted,
.request-detail-view__security-note {
  margin: 0;
  color: var(--text-secondary);
  line-height: var(--line-height-body);
}
.request-detail-view__security-note {
  padding: var(--space-3) var(--space-4);
  border-radius: var(--radius-md);
  background: var(--color-surface-subtle);
}
@media (width <= 1100px) {
  .request-detail-view__grid {
    grid-template-columns: 1fr;
  }
}
</style>
