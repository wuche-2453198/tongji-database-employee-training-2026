<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppButton from '@/components/common/AppButton.vue'
import AppDescriptions from '@/components/common/AppDescriptions.vue'
import ConfirmDialog from '@/components/common/ConfirmDialog.vue'
import PageHeader from '@/components/common/PageHeader.vue'
import PageState from '@/components/common/PageState.vue'
import ProcessTimeline from '@/components/common/ProcessTimeline.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import { getRegistrationService } from '@/services/registration'
import { getCourseService } from '@/services/course'
import { isServiceError, type UiError } from '@/types/api'
import type { Registration, RegistrationStatus } from '@/domains/registration'
import type { CourseDetail } from '@/domains/course'
import type { TimelineNode } from '@/types/ui'
const route = useRoute()
const router = useRouter()
const registration = ref<Registration | null>(null)
const course = ref<CourseDetail | null>(null)
const loading = ref(false)
const error = ref<UiError | null>(null)
const cancelVisible = ref(false)
const saving = ref(false)
const id = computed(() => String(route.params.id ?? ''))
function format(value: string | null) {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? '—'
    : new Intl.DateTimeFormat('zh-CN', { dateStyle: 'medium', timeStyle: 'short' }).format(date)
}
function label(status: RegistrationStatus) {
  return (
    {
      REGISTERED: '已报名',
      SIGNED_IN: '已签到',
      ABSENT: '缺席',
      COMPLETED: '已完成',
      CANCELED: '已取消',
      UNKNOWN: '未知状态',
    }[status] ?? '未知状态'
  )
}
function semantic(
  status: RegistrationStatus,
): 'success' | 'warning' | 'error' | 'info' | 'neutral' {
  return ({
    REGISTERED: 'info',
    SIGNED_IN: 'warning',
    ABSENT: 'error',
    COMPLETED: 'success',
    CANCELED: 'neutral',
    UNKNOWN: 'neutral',
  }[status] ?? 'neutral') as 'success' | 'warning' | 'error' | 'info' | 'neutral'
}
const timeline = computed<TimelineNode[]>(() => {
  const item = registration.value
  return [
    { title: '报名成功', meta: item ? format(item.registeredAt) : '', state: 'complete' },
    {
      title: '签到',
      meta: item?.signedInAt ? format(item.signedInAt) : '',
      summary: item?.status === 'ABSENT' ? '该员工未参加本次培训。' : undefined,
      state: item?.status === 'ABSENT' ? 'error' : item?.signedInAt ? 'complete' : 'current',
    },
    {
      title: '完成培训',
      meta: item?.completedAt ? format(item.completedAt) : '',
      state: item?.status === 'COMPLETED' ? 'complete' : 'future',
    },
  ]
})
async function load() {
  loading.value = true
  error.value = null
  try {
    registration.value = await (await getRegistrationService()).getById(id.value)
    try {
      course.value = await (await getCourseService()).getCourse(registration.value.courseId)
    } catch {
      course.value = null
    }
  } catch (caught) {
    registration.value = null
    course.value = null
    error.value = isServiceError(caught)
      ? caught.ui
      : {
          kind: 'unknown',
          code: 'UNKNOWN',
          message: '报名详情加载失败。',
          fieldErrors: [],
          retryable: false,
          resultUnknown: false,
        }
  } finally {
    loading.value = false
  }
}
async function cancel() {
  if (!registration.value) return
  saving.value = true
  try {
    await (await getRegistrationService()).cancel(registration.value.id)
    cancelVisible.value = false
    await load()
  } catch (caught) {
    if (isServiceError(caught) && caught.ui.resultUnknown) {
      cancelVisible.value = false
      await load()
    } else if (isServiceError(caught)) error.value = caught.ui
  } finally {
    saving.value = false
  }
}
onMounted(load)
</script>
<template>
  <section class="business-detail">
    <PageHeader
      title="报名详情"
      context="detail"
      :breadcrumbs="['报名管理', '报名详情']"
      @back="router.back"
      ><template #action
        ><AppButton label="刷新状态" :loading="loading" @click="load" /></template></PageHeader
    ><PageState v-if="loading" state="loading" /><PageState
      v-else-if="error?.kind === 'forbidden'"
      state="forbidden"
      :description="error.message"
    /><PageState
      v-else-if="error?.kind === 'not-found'"
      state="not-found"
      :description="error.message"
    /><PageState
      v-else-if="error"
      state="error"
      :description="error.message"
      primary-label="重新加载"
      @primary="load"
    /><template v-else-if="registration"
      ><section class="business-detail__title">
        <div>
          <p>报名记录 {{ registration.id }}</p>
          <h2>{{ registration.courseName }}</h2>
        </div>
        <StatusTag :label="label(registration.status)" :semantic="semantic(registration.status)" />
      </section>
      <section class="business-detail__panel">
        <h2>培训进度</h2>
        <ProcessTimeline :nodes="timeline" accessible-label="报名培训进度" />
      </section>
      <section class="business-detail__grid">
        <article class="business-detail__panel">
          <h2>报名信息</h2>
          <AppDescriptions
            :items="[
              { label: '报名编号', value: registration.id },
              { label: '员工', value: registration.employeeName },
              { label: '部门', value: registration.departmentName || '—' },
              { label: '报名时间', value: format(registration.registeredAt) },
              { label: '签到时间', value: format(registration.signedInAt) },
              { label: '完成时间', value: format(registration.completedAt) },
              {
                label: '签到方式',
                value:
                  registration.signinMethod === 'MANUAL'
                    ? '人工签到'
                    : registration.signinMethod === 'SELF'
                      ? '本人签到'
                      : '—',
              },
              {
                label: '培训学时',
                value: registration.actualHours ? `${registration.actualHours} 小时` : '待确认',
              },
              { label: '签到备注', value: registration.attendanceNote || '—', span: 2 },
            ]"
          />
        </article>
        <aside class="business-detail__panel">
          <h2>课程摘要</h2>
          <AppDescriptions
            v-if="course"
            :columns="1"
            :items="[
              { label: '课程类型', value: course.typeLabel },
              { label: '讲师', value: course.trainerName },
              { label: '时间', value: `${format(course.startTime)} - ${format(course.endTime)}` },
              { label: '地点', value: course.location },
            ]"
          />
          <p v-else>课程信息暂时不可用。</p>
        </aside>
      </section>
      <div v-if="registration.status === 'REGISTERED'" class="business-detail__actions">
        <AppButton
          label="取消报名"
          variant="danger"
          @click="cancelVisible = true"
        /></div></template
    ><ConfirmDialog
      v-model="cancelVisible"
      title="取消报名"
      description="取消后将释放课程名额。"
      confirm-label="确认取消"
      type="danger"
      :loading="saving"
      @confirm="cancel"
    />
  </section>
</template>
<style scoped>
.business-detail {
  display: grid;
  gap: var(--space-6);
}
.business-detail__title,
.business-detail__panel {
  padding: var(--space-6);
  border: var(--border-default);
  border-radius: var(--radius-lg);
  background: var(--color-surface);
}
.business-detail__title {
  display: flex;
  justify-content: space-between;
  gap: var(--space-4);
}
.business-detail__title p {
  margin: 0 0 var(--space-1);
  color: var(--text-secondary);
}
.business-detail h2 {
  margin: 0;
}
.business-detail__panel {
  display: grid;
  gap: var(--space-5);
}
.business-detail__grid {
  display: grid;
  grid-template-columns: minmax(0, 1.35fr) minmax(300px, 0.65fr);
  gap: var(--space-6);
}
.business-detail__actions {
  display: flex;
  justify-content: flex-end;
}
@media (width <= 1100px) {
  .business-detail__grid {
    grid-template-columns: 1fr;
  }
}
</style>
