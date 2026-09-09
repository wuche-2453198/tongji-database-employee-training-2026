<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { LatestRequestController, SingleFlightController } from '@/api/request-control'
import AppButton from '@/components/common/AppButton.vue'
import DataTable from '@/components/common/DataTable.vue'
import PageHeader from '@/components/common/PageHeader.vue'
import PageState from '@/components/common/PageState.vue'
import { getCourseService } from '@/services/course'
import { getRegistrationService } from '@/services/registration'
import { isServiceError, type UiError } from '@/types/api'
import type { CourseSummary } from '@/types/course'
import type { TableColumn } from '@/types/ui'

const route = useRoute()
const router = useRouter()
const latestQuery = new LatestRequestController()
const submissions = new SingleFlightController()

const scenarios = [
  { value: 'normal', label: '正常列表' },
  { value: 'empty', label: '空结果' },
  { value: 'failure', label: '服务失败（500）' },
  { value: 'forbidden', label: '无权访问（403）' },
  { value: 'not-found', label: '资源不存在（404）' },
  { value: 'conflict', label: '报名冲突（409）' },
  { value: 'result-unknown', label: '写操作结果未知' },
]

const selectedScenario = ref(String(route.query.scenario || 'normal'))
const loading = ref(false)
const submitting = ref(false)
const rows = ref<CourseSummary[]>([])
const error = ref<UiError | null>(null)
const actionMessage = ref('')

const columns: TableColumn[] = [
  { key: 'name', label: '课程名称', minWidth: 220 },
  { key: 'typeLabel', label: '课程类型', width: 120 },
  { key: 'trainerName', label: '讲师', width: 100 },
  { key: 'location', label: '地点', minWidth: 180 },
  { key: 'remainingSeats', label: '剩余名额', width: 100, align: 'right' },
  { key: 'statusLabel', label: '状态', width: 100, align: 'center' },
]

const tableRows = computed<Record<string, unknown>[]>(() =>
  rows.value.map((course) => ({ ...course })),
)

const errorState = computed<'error' | 'forbidden' | 'not-found'>(() => {
  if (error.value?.kind === 'forbidden') return 'forbidden'
  if (error.value?.kind === 'not-found') return 'not-found'
  return 'error'
})

async function loadCourses(): Promise<void> {
  loading.value = true
  error.value = null
  actionMessage.value = ''

  try {
    const service = await getCourseService()
    const result = await latestQuery.run((signal) =>
      service.listCourses({ page: 1, pageSize: 20 }, { signal }),
    )
    rows.value = result.items
  } catch (caught) {
    if (isServiceError(caught) && caught.ui.kind === 'canceled') return
    rows.value = []
    error.value = isServiceError(caught)
      ? caught.ui
      : {
          kind: 'unknown',
          code: 'UNKNOWN',
          message: '发生未知错误，请稍后重试。',
          fieldErrors: [],
          retryable: false,
          resultUnknown: false,
        }
  } finally {
    loading.value = false
  }
}

async function changeScenario(): Promise<void> {
  await router.replace({ query: { scenario: selectedScenario.value } })
  await loadCourses()
}

async function register(): Promise<void> {
  submitting.value = true
  error.value = null
  actionMessage.value = ''

  try {
    const service = await getRegistrationService()
    const receipt = await submissions.run('course-registration', () =>
      service.create({ courseId: rows.value[0]?.id || '4001' }),
    )
    actionMessage.value = `报名成功：${receipt.id}`
  } catch (caught) {
    error.value = isServiceError(caught)
      ? caught.ui
      : {
          kind: 'unknown',
          code: 'UNKNOWN',
          message: '发生未知错误，请稍后重试。',
          fieldErrors: [],
          retryable: false,
          resultUnknown: false,
        }
  } finally {
    submitting.value = false
  }
}

onMounted(loadCourses)
onBeforeUnmount(() => latestQuery.cancel())
</script>

<template>
  <main class="api-boundary">
    <PageHeader
      title="API 与 Mock 边界验收"
      description="页面只消费课程领域服务；传输 DTO、Base URL 与 Mock 数据均隔离在适配层内。"
      :breadcrumbs="['前端工程', '开发工具', 'M5 API 边界']"
    />

    <section class="api-boundary__controls" aria-label="Mock 场景控制">
      <label for="api-scenario">验收场景</label>
      <el-select
        id="api-scenario"
        v-model="selectedScenario"
        aria-label="验收场景"
        @change="changeScenario"
      >
        <el-option
          v-for="scenario in scenarios"
          :key="scenario.value"
          :label="scenario.label"
          :value="scenario.value"
        />
      </el-select>
      <AppButton label="重新查询" :loading="loading" @click="loadCourses" />
      <AppButton
        label="模拟报名"
        variant="primary"
        :loading="submitting"
        loading-text="提交中…"
        @click="register"
      />
    </section>

    <el-alert
      v-if="actionMessage"
      data-testid="action-success"
      :title="actionMessage"
      type="success"
      show-icon
      :closable="false"
    />

    <el-alert
      v-if="error"
      data-testid="ui-error"
      :title="error.resultUnknown ? '操作结果未知' : error.message"
      :description="error.resultUnknown ? error.message : `错误码：${error.code}`"
      :type="error.resultUnknown ? 'warning' : 'error'"
      show-icon
      :closable="false"
    />

    <section class="api-boundary__content" aria-live="polite">
      <PageState v-if="loading" state="loading" compact />
      <PageState
        v-else-if="error"
        :state="errorState"
        :title="error.resultUnknown ? '请查询最终状态' : undefined"
        :description="error.message"
        :trace-id="error.traceId"
        :primary-label="error.retryable ? '重新加载' : ''"
        :hide-primary="!error.retryable"
        compact
        @primary="loadCourses"
      />
      <PageState
        v-else-if="rows.length === 0"
        state="empty"
        title="暂无课程"
        description="服务已成功返回空列表，页面没有读取任何原始 Mock 字段。"
        compact
      />
      <DataTable v-else :columns="columns" :rows="tableRows" />
    </section>

    <aside class="api-boundary__rules">
      <strong>当前边界</strong>
      <span>查询故障最多重试一次</span>
      <span>写操作不自动重试</span>
      <span>超时/断线按结果未知处理</span>
      <span>重复提交共享同一在途请求</span>
    </aside>
  </main>
</template>

<style scoped>
.api-boundary {
  display: grid;
  width: min(var(--layout-content-max-width), calc(100% - 48px));
  gap: var(--space-6);
  margin: 0 auto;
  padding: var(--space-8) 0 var(--space-12);
}

.api-boundary__controls,
.api-boundary__rules {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-4);
  border: var(--border-default);
  border-radius: var(--radius-lg);
  background: var(--color-surface);
}

.api-boundary__controls label {
  color: var(--text-secondary);
  font-size: var(--font-size-helper);
  font-weight: 600;
}

.api-boundary__controls :deep(.el-select) {
  width: 240px;
}

.api-boundary__content {
  min-height: 220px;
}

.api-boundary__rules {
  color: var(--text-secondary);
  font-size: var(--font-size-helper);
}

.api-boundary__rules span::before {
  margin-right: var(--space-2);
  color: var(--color-success);
  content: '✓';
}
</style>
