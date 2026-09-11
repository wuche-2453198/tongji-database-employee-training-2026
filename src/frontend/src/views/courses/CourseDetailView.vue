<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { LatestRequestController, SingleFlightController } from '@/api/request-control'
import AppButton from '@/components/common/AppButton.vue'
import AppDescriptions from '@/components/common/AppDescriptions.vue'
import ConfirmDialog from '@/components/common/ConfirmDialog.vue'
import CourseSummary from '@/components/business/CourseSummary.vue'
import PageHeader from '@/components/common/PageHeader.vue'
import PageState from '@/components/common/PageState.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import { hasAnyRole } from '@/config/permissions'
import { getCourseService } from '@/services/course'
import { getRegistrationService } from '@/services/registration'
import { useAuthStore } from '@/stores/auth'
import { resolveCourseCover } from '@/utils/course-cover'
import { isServiceError, type UiError } from '@/types/api'
import type { CourseDetail } from '@/domains/course'
import type { CourseInfo } from '@/types/ui'
import type { CourseStatus } from '@/domains/course'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const latestQuery = new LatestRequestController()
const submissions = new SingleFlightController()
const course = ref<CourseDetail | null>(null)
const loading = ref(false)
const error = ref<UiError | null>(null)
const submitting = ref(false)
const actionMessage = ref('')
const resultUnknown = ref(false)
const conflictMessage = computed(() =>
  error.value?.kind === 'conflict' ? error.value.message : '',
)

const courseId = computed(() => (typeof route.params.id === 'string' ? route.params.id : ''))
const isEmployee = computed(() => authStore.can('request.create'))
const canPublish = computed(
  () => course.value?.status === 'DRAFT' && hasAnyRole(authStore.currentUser, ['HR', 'ADMIN']),
)
const canClose = computed(
  () => course.value?.status === 'PUBLISHED' && hasAnyRole(authStore.currentUser, ['HR', 'ADMIN']),
)
const publishing = ref(false)
const publishDialogVisible = ref(false)
const closing = ref(false)
const closeDialogVisible = ref(false)
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
        cover: resolveCourseCover(course.value.type),
      }
    : null,
)
const summaryState = computed<'published' | 'closed' | 'full' | 'restricted'>(() => {
  if (!course.value || course.value.status === 'CLOSED') return 'closed'
  if (course.value.remainingSeats !== null && course.value.remainingSeats <= 0) return 'full'
  if (
    isEmployee.value &&
    !course.value.eligibility.apply.allowed &&
    !course.value.eligibility.register.allowed
  )
    return 'restricted'
  return 'published'
})

function formatDateTime(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '—'
  return new Intl.DateTimeFormat('zh-CN', { dateStyle: 'medium', timeStyle: 'short' }).format(date)
}

function statusSemantic(status: CourseStatus): 'success' | 'warning' | 'error' | 'neutral' {
  if (status === 'PUBLISHED') return 'success'
  if (status === 'DRAFT') return 'warning'
  if (status === 'CLOSED') return 'neutral'
  return 'error'
}

async function loadCourse(): Promise<void> {
  if (!courseId.value) {
    error.value = {
      kind: 'not-found',
      code: 'INVALID_COURSE_ID',
      message: '课程编号无效。',
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
    error.value = isServiceError(caught) ? caught.ui : null
  } finally {
    loading.value = false
  }
}

function goBack(): void {
  if (window.history.length > 1) router.back()
  else void router.replace({ name: 'course-list' })
}

function startApplication(): void {
  void router.push({ name: 'request-create', query: { courseId: courseId.value } })
}

async function confirmRegistration(): Promise<void> {
  if (!course.value?.eligibility.register.allowed) return
  submitting.value = true
  actionMessage.value = ''
  resultUnknown.value = false
  try {
    const service = await getRegistrationService()
    const registration = await submissions.run(`register:${courseId.value}`, () =>
      service.create({ courseId: courseId.value }),
    )
    actionMessage.value = `报名成功，报名编号：${registration.id}`
    await loadCourse()
  } catch (caught) {
    if (isServiceError(caught) && caught.ui.resultUnknown) {
      const service = await getRegistrationService()
      const finalRegistration = await service.getMineByCourse(courseId.value)
      if (finalRegistration) {
        actionMessage.value = `已确认报名，报名编号：${finalRegistration.id}`
        await loadCourse()
      } else {
        resultUnknown.value = true
      }
    } else if (isServiceError(caught)) {
      error.value = caught.ui
      if (caught.ui.kind === 'conflict') {
        const conflict = caught.ui
        await loadCourse()
        error.value = conflict
      }
    } else {
      error.value = {
        kind: 'unknown',
        code: 'UNKNOWN',
        message: '发生未知错误，请稍后重试。',
        fieldErrors: [],
        retryable: false,
        resultUnknown: false,
      }
    }
  } finally {
    submitting.value = false
  }
}

function openPublishDialog(): void {
  publishDialogVisible.value = true
}

function openCloseDialog(): void {
  closeDialogVisible.value = true
}

async function confirmClose(): Promise<void> {
  closing.value = true
  actionMessage.value = ''
  try {
    const service = await getCourseService()
    await service.closeCourse(courseId.value)
    closeDialogVisible.value = false
    actionMessage.value = '课程已关闭。'
    await loadCourse()
  } catch (caught) {
    if (isServiceError(caught)) {
      if (caught.ui.kind === 'conflict') await loadCourse()
      else error.value = caught.ui
    } else {
      error.value = {
        kind: 'unknown',
        code: 'UNKNOWN',
        message: '关闭失败，请稍后重试。',
        fieldErrors: [],
        retryable: false,
        resultUnknown: false,
      }
    }
  } finally {
    closing.value = false
  }
}

async function confirmPublish(): Promise<void> {
  publishing.value = true
  actionMessage.value = ''
  try {
    const service = await getCourseService()
    await service.publishCourse(courseId.value)
    publishDialogVisible.value = false
    actionMessage.value = '课程已发布。'
    await loadCourse()
  } catch (caught) {
    if (isServiceError(caught)) {
      if (caught.ui.kind === 'conflict') {
        // 状态已被并发修改，刷新展示最新状态即可。
        await loadCourse()
      } else {
        error.value = caught.ui
      }
    } else {
      error.value = {
        kind: 'unknown',
        code: 'UNKNOWN',
        message: '发布失败，请稍后重试。',
        fieldErrors: [],
        retryable: false,
        resultUnknown: false,
      }
    }
  } finally {
    publishing.value = false
  }
}

onMounted(loadCourse)
onBeforeUnmount(() => latestQuery.cancel())
</script>

<template>
  <section class="course-detail-view">
    <PageHeader title="课程详情" context="detail" @back="goBack" />
    <PageState v-if="loading" state="loading" />
    <PageState
      v-else-if="error?.kind === 'forbidden'"
      state="forbidden"
      :description="error.message"
      secondary-label="返回课程中心"
      compact
      @secondary="goBack"
    />
    <PageState
      v-else-if="error?.kind === 'not-found'"
      state="not-found"
      :description="error.message"
      secondary-label="返回课程中心"
      compact
      @secondary="goBack"
    />
    <PageState
      v-else-if="error"
      state="error"
      :description="error.message"
      :trace-id="error.traceId"
      primary-label="重新加载"
      @primary="loadCourse"
    />
    <template v-else-if="course && courseInfo">
      <CourseSummary
        :course="courseInfo"
        :state="summaryState"
        :restriction-reason="course.eligibility.apply.reason || course.eligibility.register.reason"
      >
        <template #action>
          <div class="course-detail-view__actions">
            <AppButton
              v-if="isEmployee"
              label="提交培训申请"
              variant="primary"
              :disabled="!course.eligibility.apply.allowed"
              :disabled-reason="course.eligibility.apply.reason"
              @click="startApplication"
            />
            <AppButton
              v-if="isEmployee"
              label="报名课程"
              :disabled="!course.eligibility.register.allowed || submitting"
              :disabled-reason="course.eligibility.register.reason"
              :loading="submitting"
              loading-text="报名中…"
              @click="confirmRegistration"
            />
            <AppButton
              v-if="canPublish"
              label="发布课程"
              variant="primary"
              @click="openPublishDialog"
            />
            <AppButton v-if="canClose" label="关闭课程" variant="danger" @click="openCloseDialog" />
          </div>
        </template>
      </CourseSummary>

      <el-alert
        v-if="actionMessage"
        :title="actionMessage"
        type="success"
        show-icon
        :closable="false"
      />
      <el-alert
        v-if="resultUnknown"
        title="报名结果未知"
        description="请查询最新报名状态后再继续操作，系统不会自动重复提交。"
        type="warning"
        show-icon
        :closable="false"
      />
      <el-alert
        v-if="conflictMessage"
        :title="conflictMessage"
        description="课程详情已刷新，请根据最新资格决定下一步。"
        type="warning"
        show-icon
        :closable="false"
      />

      <section class="course-detail-view__grid">
        <article class="course-detail-view__panel">
          <div class="course-detail-view__panel-title">
            <h2>课程信息</h2>
            <StatusTag :label="course.statusLabel" :semantic="statusSemantic(course.status)" />
          </div>
          <AppDescriptions
            :items="[
              { label: '课程编号', value: course.id },
              {
                label: '剩余名额',
                value:
                  course.remainingSeats === null
                    ? '名额数据暂不可用'
                    : `${course.remainingSeats} / ${course.maxStudents}`,
              },
              { label: '主办部门', value: course.organizer },
            ]"
          />
          <h3>课程介绍</h3>
          <p class="course-detail-view__copy">{{ course.description }}</p>
          <h3>学习目标</h3>
          <ul class="course-detail-view__list">
            <li v-for="objective in course.objectives" :key="objective">{{ objective }}</li>
          </ul>
        </article>
        <aside class="course-detail-view__panel">
          <h2>培训讲师</h2>
          <div class="course-detail-view__trainer">
            <div class="course-detail-view__avatar">{{ course.trainer.name.slice(0, 1) }}</div>
            <div>
              <strong>{{ course.trainer.name }}</strong>
              <p>{{ course.trainer.title }}</p>
            </div>
          </div>
          <AppDescriptions
            :items="[
              { label: '所属部门', value: course.trainer.department },
              { label: '擅长领域', value: course.trainer.expertise },
            ]"
            :columns="1"
          />
          <div class="course-detail-view__rating">
            <span class="course-detail-view__rating-label">课程评分</span>
            <el-rate
              v-if="course.trainer.rating !== undefined"
              :model-value="course.trainer.rating"
              disabled
              allow-half
            />
            <span
              v-if="course.trainer.rating !== undefined"
              class="course-detail-view__rating-value"
            >
              {{ course.trainer.rating.toFixed(1) }} / 5
            </span>
            <span v-else class="course-detail-view__rating-value">暂无评分</span>
          </div>
          <h2>课程资料</h2>
          <ul class="course-detail-view__materials">
            <li v-for="material in course.materials" :key="material.id">
              <span>{{ material.name }}</span
              ><StatusTag
                v-if="!material.available"
                label="报名后开放"
                semantic="neutral"
                :show-dot="false"
              />
            </li>
            <li v-if="!course.materials.length">暂无课程资料</li>
          </ul>
        </aside>
      </section>
      <p v-if="!isEmployee" class="course-detail-view__browse-note">
        当前角色仅可浏览课程详情，申请与报名入口仅对员工本人开放。
      </p>
    </template>

    <ConfirmDialog
      v-model="publishDialogVisible"
      title="发布课程"
      :description="`确认发布课程「${course?.name ?? ''}」吗？发布后将占用主办部门预算，且不可恢复为草稿。`"
      confirm-label="确认发布"
      :loading="publishing"
      loading-text="发布中…"
      @confirm="confirmPublish"
    />

    <ConfirmDialog
      v-model="closeDialogVisible"
      title="关闭课程"
      :description="`确定关闭课程「${course?.name ?? ''}」吗？关闭后员工将无法继续报名。`"
      confirm-label="确认关闭"
      type="danger"
      :loading="closing"
      loading-text="关闭中…"
      @confirm="confirmClose"
    />
  </section>
</template>

<style scoped>
.course-detail-view {
  display: grid;
  gap: var(--space-6);
}
.course-detail-view__actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: var(--space-3);
}
.course-detail-view__grid {
  display: grid;
  grid-template-columns: minmax(0, 1.35fr) minmax(300px, 0.65fr);
  gap: var(--space-6);
}
.course-detail-view__panel {
  display: grid;
  gap: var(--space-4);
  padding: var(--space-6);
  border: var(--border-default);
  border-radius: var(--radius-lg);
  background: var(--color-surface);
}
.course-detail-view__panel-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
}
.course-detail-view h2,
.course-detail-view h3 {
  margin: 0;
  font-size: var(--font-size-title-component);
  line-height: var(--line-height-title-component);
}
.course-detail-view h3 {
  margin-top: var(--space-2);
  font-size: var(--font-size-body);
}
.course-detail-view__copy {
  margin: 0;
  color: var(--text-secondary);
  line-height: var(--line-height-body);
}
.course-detail-view__list {
  display: grid;
  gap: var(--space-2);
  margin: 0;
  padding-left: var(--space-5);
  color: var(--text-secondary);
  line-height: var(--line-height-body);
}
.course-detail-view__trainer {
  display: flex;
  align-items: center;
  gap: var(--space-3);
}
.course-detail-view__avatar {
  display: grid;
  width: 44px;
  height: 44px;
  place-items: center;
  border-radius: 50%;
  color: var(--text-on-primary);
  background: var(--color-primary);
  font-size: 18px;
  font-weight: 700;
}
.course-detail-view__trainer p {
  margin: var(--space-1) 0 0;
  color: var(--text-secondary);
}
.course-detail-view__rating {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}
.course-detail-view__rating-label {
  color: var(--text-secondary);
}
.course-detail-view__rating-value {
  color: var(--text-secondary);
  font-size: var(--font-size-caption);
}
.course-detail-view__materials {
  display: grid;
  gap: var(--space-2);
  padding: 0;
  margin: 0;
  list-style: none;
}
.course-detail-view__materials li {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
  padding: var(--space-2) 0;
  border-bottom: var(--border-default);
  color: var(--text-secondary);
}
.course-detail-view__browse-note {
  padding: var(--space-3) var(--space-4);
  margin: 0;
  border-radius: var(--radius-md);
  color: var(--text-secondary);
  background: var(--color-surface-subtle);
}
@media (width <= 1100px) {
  .course-detail-view__grid {
    grid-template-columns: 1fr;
  }
}
</style>
