<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import AppButton from '@/components/common/AppButton.vue'
import AppPagination from '@/components/common/AppPagination.vue'
import ConfirmDialog from '@/components/common/ConfirmDialog.vue'
import DataTable from '@/components/common/DataTable.vue'
import PageHeader from '@/components/common/PageHeader.vue'
import PageState from '@/components/common/PageState.vue'
import SearchPanel from '@/components/common/SearchPanel.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import { getCertificateService } from '@/services/certificate'
import { isServiceError, type UiError } from '@/types/api'
import type { Certificate, CertificateCandidate, CertificateQuery } from '@/domains/certificate'
import type { TableColumn } from '@/types/ui'
const router = useRouter()
const filters = reactive({ employeeKeyword: '', keyword: '', page: 1, pageSize: 20 })
const candidates = ref<CertificateCandidate[]>([])
const issued = ref<Certificate[]>([])
const total = ref(0)
const loading = ref(false)
const hasLoaded = ref(false)
const error = ref<UiError | null>(null)
const selected = ref<CertificateCandidate | null>(null)
const dialogVisible = ref(false)
const saving = ref(false)
const candidateColumns: TableColumn[] = [
  { key: 'employeeName', label: '员工', width: 130 },
  { key: 'departmentName', label: '部门', width: 140 },
  { key: 'courseName', label: '课程名称', minWidth: 260 },
  { key: 'actualHoursLabel', label: '实际学时', width: 120 },
  { key: 'qualificationLabel', label: '资格', width: 150 },
]
const issuedColumns: TableColumn[] = [
  { key: 'certificateNo', label: '证书编号', width: 190 },
  { key: 'employeeName', label: '员工', width: 130 },
  { key: 'courseName', label: '课程名称', minWidth: 240 },
  { key: 'issuedAtLabel', label: '生成时间', width: 170 },
  { key: 'statusLabel', label: '状态', width: 100 },
]
const state = computed(() =>
  loading.value && !hasLoaded.value
    ? 'loading'
    : !loading.value && candidates.value.length === 0 && issued.value.length === 0
      ? filters.employeeKeyword || filters.keyword
        ? 'no-result'
        : 'empty'
      : 'default',
)
const candidateRows = computed(() =>
  candidates.value.map((item) => ({
    ...item,
    actualHoursLabel: item.actualHours ? `${item.actualHours} 小时` : '待确认',
    qualificationLabel: item.qualification.allowed
      ? '可生成'
      : item.qualification.reason || '不可生成',
  })),
)
const issuedRows = computed(() =>
  issued.value.map((item) => ({
    ...item,
    issuedAtLabel: format(item.issuedAt),
    statusLabel: item.displayStatus === 'VALID' ? '有效' : item.displayStatus,
  })),
)
function format(value: string | null) {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? '—'
    : new Intl.DateTimeFormat('zh-CN', { dateStyle: 'short' }).format(date)
}
function query(): CertificateQuery {
  return {
    employeeKeyword: filters.employeeKeyword || undefined,
    keyword: filters.keyword || undefined,
    page: filters.page,
    pageSize: filters.pageSize,
    sortBy: 'issuedAt',
    sortDirection: 'desc',
  }
}
async function load() {
  loading.value = true
  error.value = null
  try {
    const service = await getCertificateService()
    const [candidateResult, issuedResult] = await Promise.all([
      service.listCandidates(query()),
      service.listManage(query()),
    ])
    candidates.value = candidateResult.items
    issued.value = issuedResult.items
    total.value = candidateResult.total
    hasLoaded.value = true
  } catch (caught) {
    candidates.value = []
    issued.value = []
    hasLoaded.value = true
    error.value = isServiceError(caught)
      ? caught.ui
      : {
          kind: 'unknown',
          code: 'UNKNOWN',
          message: '证书管理数据加载失败。',
          fieldErrors: [],
          retryable: false,
          resultUnknown: false,
        }
  } finally {
    loading.value = false
  }
}
async function sync(next: Partial<typeof filters>, reset = false) {
  Object.assign(filters, next)
  if (reset) filters.page = 1
  await load()
}
function open(row: CertificateCandidate) {
  selected.value = row
  dialogVisible.value = true
}
async function generate() {
  if (!selected.value) return
  saving.value = true
  try {
    const certificate = await (
      await getCertificateService()
    ).generate({ registrationId: selected.value.registrationId })
    dialogVisible.value = false
    await load()
    void router.push({ name: 'certificate-detail', params: { id: certificate.id } })
  } catch (caught) {
    if (isServiceError(caught) && caught.ui.resultUnknown) {
      dialogVisible.value = false
      await load()
    } else if (isServiceError(caught)) error.value = caught.ui
  } finally {
    saving.value = false
  }
}
onMounted(load)
</script>
<template>
  <section class="business-list">
    <PageHeader title="证书管理" description="为完成培训且满足条件的员工生成证书。" /><SearchPanel
      :expanded="false"
      :searching="loading"
      @search="sync({}, true)"
      @reset="sync({ employeeKeyword: '', keyword: '', page: 1, pageSize: 20 }, true)"
      ><el-input
        v-model="filters.employeeKeyword"
        clearable
        aria-label="员工"
        placeholder="员工姓名或工号" /><el-input
        v-model="filters.keyword"
        clearable
        aria-label="课程"
        placeholder="课程名称" /></SearchPanel
    ><PageState v-if="state === 'loading'" state="loading" /><PageState
      v-else-if="error?.kind === 'forbidden'"
      state="forbidden"
      :description="error.message"
    /><PageState
      v-else-if="error"
      state="error"
      :description="error.message"
      primary-label="重新加载"
      @primary="load"
    /><PageState
      v-else-if="state === 'empty'"
      state="empty"
      title="暂无证书候选或已生成证书"
      description="完成培训的记录满足条件后，会出现在候选列表。"
      compact
    /><PageState
      v-else-if="state === 'no-result'"
      state="no-result"
      secondary-label="清空筛选"
      compact
      @secondary="sync({ employeeKeyword: '', keyword: '', page: 1 }, true)"
    /><template v-else
      ><section class="business-list__section">
        <h2>待生成证书</h2>
        <DataTable :columns="candidateColumns" :rows="candidateRows" has-actions
          ><template #cell="{ column, row }"
            ><StatusTag
              v-if="column.key === 'qualificationLabel'"
              :label="String(row.qualificationLabel)"
              :semantic="row.qualification?.allowed ? 'success' : 'warning'"
            /><span v-else>{{ row[column.key] || '—' }}</span></template
          ><template #actions="{ row }"
            ><AppButton
              label="生成证书"
              variant="text"
              :disabled="!row.qualification?.allowed"
              @click="open(row as unknown as CertificateCandidate)" /></template
        ></DataTable>
      </section>
      <section v-if="issued.length" class="business-list__section">
        <h2>已生成证书</h2>
        <DataTable :columns="issuedColumns" :rows="issuedRows" has-actions
          ><template #actions="{ row }"
            ><AppButton
              label="查看"
              variant="text"
              @click="
                router.push({ name: 'certificate-detail', params: { id: row.id } })
              " /></template
        ></DataTable>
      </section>
      <AppPagination
        v-if="total"
        :page="filters.page"
        :page-size="filters.pageSize"
        :total="total"
        :disabled="loading"
        @update:page="sync({ page: $event })"
        @update:page-size="sync({ pageSize: $event }, true)" /></template
    ><ConfirmDialog
      v-model="dialogVisible"
      title="生成培训证书"
      :description="
        selected ? `将为 ${selected.employeeName} 生成《${selected.courseName}》证书。` : ''
      "
      confirm-label="确认生成"
      :loading="saving"
      @confirm="generate"
    />
  </section>
</template>
<style scoped>
.business-list {
  display: grid;
  gap: var(--space-6);
}
.business-list__section {
  display: grid;
  gap: var(--space-4);
}
.business-list__section h2 {
  margin: 0;
  font-size: var(--font-size-title-component);
}
</style>
