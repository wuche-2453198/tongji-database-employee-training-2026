<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'

import AppButton from '@/components/common/AppButton.vue'
import AppPagination from '@/components/common/AppPagination.vue'
import DataTable from '@/components/common/DataTable.vue'
import FormField from '@/components/common/FormField.vue'
import PageHeader from '@/components/common/PageHeader.vue'
import PageState from '@/components/common/PageState.vue'
import SearchPanel from '@/components/common/SearchPanel.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import { hasAnyRole } from '@/config/permissions'
import { getBlacklistService } from '@/services/blacklist'
import { useAuthStore } from '@/stores/auth'
import { isServiceError, type UiError } from '@/types/api'
import type {
  BlacklistCandidate,
  BlacklistQuery,
  BlacklistRecord,
  BlacklistStatus,
} from '@/domains/blacklist'
import type { TableColumn } from '@/types/ui'

const authStore = useAuthStore()

const canManage = computed(() => hasAnyRole(authStore.currentUser, ['DEPT_MANAGER', 'ADMIN']))

const filters = reactive<{ status: BlacklistStatus | ''; page: number; pageSize: number }>({
  status: '',
  page: 1,
  pageSize: 20,
})

const records = ref<BlacklistRecord[]>([])
const total = ref(0)
const loading = ref(false)
const hasLoaded = ref(false)
const error = ref<UiError | null>(null)

const columns: TableColumn[] = [
  { key: 'empName', label: '员工', width: 110 },
  { key: 'empId', label: '工号', width: 90 },
  { key: 'deptName', label: '部门', width: 130 },
  { key: 'reason', label: '原因', minWidth: 200 },
  { key: 'startDateLabel', label: '开始日期', width: 120 },
  { key: 'endDateLabel', label: '结束日期', width: 120 },
  { key: 'statusLabel', label: '状态', width: 100 },
]

const statusOptions = [
  { label: '全部', value: '' },
  { label: '生效中', value: 'ACTIVE' },
  { label: '已解除', value: 'RELEASED' },
]

const state = computed(() => {
  if (loading.value && !hasLoaded.value) return 'loading'
  if (error.value) return 'error'
  if (!loading.value && records.value.length === 0) return 'empty'
  return 'default'
})

const rows = computed(() =>
  records.value.map((record) => ({
    ...record,
    empId: String(record.empId),
    startDateLabel: formatDate(record.startDate),
    endDateLabel: formatDate(record.endDate),
  })),
)

const dialogVisible = ref(false)
const saving = ref(false)
const submitError = ref<UiError | null>(null)
const successMessage = ref('')
const editing = ref<BlacklistRecord | null>(null)
const editSaving = ref(false)
const editError = ref('')
const editForm = reactive<{ reason: string; endDate: string; status: 'ACTIVE' | 'RELEASED' }>({
  reason: '',
  endDate: '',
  status: 'ACTIVE',
})
const deleting = ref<BlacklistRecord | null>(null)
const editingVisible = computed({
  get: () => editing.value !== null,
  set: (value: boolean) => {
    if (!value && !editSaving.value) editing.value = null
  },
})

const employees = ref<BlacklistCandidate[]>([])
const loadingEmployees = ref(false)

const form = reactive<{
  empId: number | null
  reason: string
  startDate: string
  endDate: string
}>({
  empId: null,
  reason: '',
  startDate: '',
  endDate: '',
})

const selectedEmployee = computed(
  () => employees.value.find((employee) => employee.empId === form.empId) ?? null,
)

function formatDate(value: string | null): string {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? '—'
    : new Intl.DateTimeFormat('zh-CN', { dateStyle: 'short' }).format(date)
}

function todayIso(): string {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

function buildQuery(): BlacklistQuery {
  return {
    status: filters.status || undefined,
    page: filters.page,
    pageSize: filters.pageSize,
    sortBy: 'blackId',
    sortDirection: 'desc',
  }
}

async function load(): Promise<void> {
  loading.value = true
  error.value = null
  try {
    const service = await getBlacklistService()
    const result = await service.listBlacklists(buildQuery())
    records.value = result.items
    total.value = result.total
    hasLoaded.value = true
  } catch (caught) {
    records.value = []
    total.value = 0
    hasLoaded.value = true
    error.value = isServiceError(caught)
      ? caught.ui
      : {
          kind: 'unknown',
          code: 'UNKNOWN',
          message: '黑名单数据加载失败。',
          fieldErrors: [],
          retryable: false,
          resultUnknown: false,
        }
  } finally {
    loading.value = false
  }
}

async function sync(next: Partial<typeof filters>, reset = false): Promise<void> {
  Object.assign(filters, next)
  if (reset) filters.page = 1
  await load()
}

async function openDialog(): Promise<void> {
  submitError.value = null
  successMessage.value = ''
  form.empId = null
  form.reason = ''
  form.startDate = ''
  form.endDate = ''
  dialogVisible.value = true
  await loadEmployees()
}

async function loadEmployees(): Promise<void> {
  loadingEmployees.value = true
  try {
    const service = await getBlacklistService()
    const result = await service.listCandidates({ page: 1, pageSize: 200 })
    employees.value = result.items
  } catch {
    employees.value = []
  } finally {
    loadingEmployees.value = false
  }
}

function validateForm(): string | null {
  if (form.empId === null) return '请选择要加入黑名单的员工。'
  const reason = form.reason.trim()
  if (!reason) return '请填写黑名单原因。'
  if (reason.length > 500) return '黑名单原因不能超过 500 字。'
  if (form.endDate) {
    if (form.endDate <= todayIso()) return '结束日期必须晚于今天。'
    if (form.startDate && form.endDate < form.startDate) {
      return '结束日期不能早于开始日期。'
    }
  }
  return null
}

async function submit(): Promise<void> {
  const validationMessage = validateForm()
  if (validationMessage) {
    submitError.value = {
      kind: 'validation',
      code: 'VALIDATION',
      message: validationMessage,
      fieldErrors: [],
      retryable: false,
      resultUnknown: false,
    }
    return
  }

  saving.value = true
  submitError.value = null
  try {
    const service = await getBlacklistService()
    await service.createBlacklist({
      empId: form.empId as number,
      reason: form.reason.trim(),
      startDate: form.startDate || undefined,
      endDate: form.endDate || undefined,
    })
    dialogVisible.value = false
    successMessage.value = '已将员工加入黑名单。'
    await load()
  } catch (caught) {
    submitError.value = isServiceError(caught)
      ? caught.ui
      : {
          kind: 'unknown',
          code: 'UNKNOWN',
          message: '加入黑名单失败，请稍后重试。',
          fieldErrors: [],
          retryable: false,
          resultUnknown: false,
        }
  } finally {
    saving.value = false
  }
}

function openEdit(record: BlacklistRecord) {
  editing.value = record
  editError.value = ''
  editForm.reason = record.reason
  editForm.endDate = record.endDate?.slice(0, 10) ?? ''
  editForm.status = record.status === 'RELEASED' ? 'RELEASED' : 'ACTIVE'
}

async function saveEdit() {
  if (!editing.value) return
  if (!editForm.reason.trim()) {
    editError.value = '请填写黑名单原因。'
    return
  }
  editSaving.value = true
  editError.value = ''
  try {
    await (
      await getBlacklistService()
    ).updateBlacklist(editing.value.id, {
      reason: editForm.reason.trim(),
      endDate: editForm.endDate || null,
      status: editForm.status,
    })
    editing.value = null
    successMessage.value = '黑名单记录已更新。'
    await load()
  } catch (caught) {
    editError.value = isServiceError(caught) ? caught.ui.message : '更新黑名单记录失败。'
  } finally {
    editSaving.value = false
  }
}

async function remove() {
  if (!deleting.value) return
  editSaving.value = true
  try {
    await (await getBlacklistService()).deleteBlacklist(deleting.value.id)
    deleting.value = null
    successMessage.value = '黑名单记录已删除。'
    await load()
  } catch (caught) {
    error.value = isServiceError(caught) ? caught.ui : error.value
  } finally {
    editSaving.value = false
  }
}

onMounted(load)
</script>

<template>
  <section class="blacklist-view">
    <PageHeader title="黑名单管理" description="将违规员工加入黑名单，限制其培训申请与报名。" />

    <el-alert
      v-if="successMessage"
      :title="successMessage"
      type="success"
      show-icon
      :closable="false"
    />

    <SearchPanel
      :expanded="false"
      :searching="loading"
      @search="sync({}, true)"
      @reset="sync({ status: '', page: 1, pageSize: 20 }, true)"
    >
      <el-select v-model="filters.status" clearable aria-label="黑名单状态" placeholder="状态">
        <el-option
          v-for="option in statusOptions"
          :key="option.value"
          :label="option.label"
          :value="option.value"
        />
      </el-select>
    </SearchPanel>

    <div class="blacklist-view__toolbar">
      <AppButton v-if="canManage" label="加入黑名单" variant="primary" @click="openDialog" />
    </div>

    <PageState v-if="state === 'loading'" state="loading" />
    <PageState
      v-else-if="error?.kind === 'forbidden'"
      state="forbidden"
      :description="error.message"
    />
    <PageState
      v-else-if="error"
      state="error"
      :description="error.message"
      primary-label="重新加载"
      @primary="load"
    />
    <PageState
      v-else-if="state === 'empty'"
      state="empty"
      title="暂无黑名单记录"
      description="当前范围内还没有黑名单记录。"
      compact
    />
    <template v-else>
      <DataTable :columns="columns" :rows="rows" :has-actions="canManage">
        <template #cell="{ column, row }">
          <StatusTag
            v-if="column.key === 'statusLabel'"
            :label="String(row.statusLabel)"
            :semantic="row.status === 'ACTIVE' ? 'warning' : 'neutral'"
          />
          <span v-else>{{ row[column.key] || '—' }}</span>
        </template>
        <template #actions="{ row }">
          <AppButton
            v-if="authStore.currentUser?.roles.includes('ADMIN')"
            label="编辑"
            variant="text"
            @click="openEdit(row as unknown as BlacklistRecord)"
          />
          <AppButton
            v-if="authStore.currentUser?.roles.includes('ADMIN') && row.status === 'ACTIVE'"
            label="解除"
            variant="text"
            @click="openEdit({ ...(row as unknown as BlacklistRecord), status: 'RELEASED' })"
          />
          <AppButton
            v-if="authStore.currentUser?.roles.includes('ADMIN')"
            label="删除"
            variant="text"
            @click="deleting = row as unknown as BlacklistRecord"
          />
        </template>
      </DataTable>

      <AppPagination
        v-if="total"
        :page="filters.page"
        :page-size="filters.pageSize"
        :total="total"
        :disabled="loading"
        @update:page="sync({ page: $event })"
        @update:page-size="sync({ pageSize: $event }, true)"
      />
    </template>

    <el-dialog
      v-model="dialogVisible"
      title="加入黑名单"
      width="520px"
      :close-on-click-modal="!saving"
      :close-on-press-escape="!saving"
      :show-close="!saving"
      destroy-on-close
    >
      <el-alert
        v-if="submitError"
        :title="submitError.message"
        type="error"
        show-icon
        :closable="false"
        class="blacklist-view__dialog-error"
      />

      <el-form label-position="top" @submit.prevent="submit">
        <FormField label="员工" required>
          <el-select
            v-model="form.empId"
            filterable
            :loading="loadingEmployees"
            placeholder="请选择本部门员工"
            style="width: 100%"
          >
            <el-option
              v-for="employee in employees"
              :key="employee.empId"
              :label="`${employee.empName}（工号 ${employee.empId}）· ${employee.deptName}`"
              :value="employee.empId"
            />
          </el-select>
          <p v-if="selectedEmployee" class="blacklist-view__selected">
            已选：{{ selectedEmployee.empName }} / 工号 {{ selectedEmployee.empId }} /
            {{ selectedEmployee.deptName }}
          </p>
        </FormField>

        <FormField label="黑名单原因" required>
          <el-input
            v-model="form.reason"
            type="textarea"
            :rows="4"
            maxlength="500"
            show-word-limit
            placeholder="请填写加入黑名单的原因"
          />
        </FormField>

        <div class="blacklist-view__date-row">
          <FormField label="开始日期">
            <el-date-picker
              v-model="form.startDate"
              type="date"
              value-format="YYYY-MM-DD"
              placeholder="默认今天"
              style="width: 100%"
            />
          </FormField>
          <FormField label="结束日期">
            <el-date-picker
              v-model="form.endDate"
              type="date"
              value-format="YYYY-MM-DD"
              placeholder="默认 30 天后"
              style="width: 100%"
            />
          </FormField>
        </div>
      </el-form>

      <template #footer>
        <div class="blacklist-view__actions">
          <AppButton label="取消" :disabled="saving" @click="dialogVisible = false" />
          <AppButton
            label="确认加入"
            variant="primary"
            :loading="saving"
            loading-text="提交中…"
            @click="submit"
          />
        </div>
      </template>
    </el-dialog>
    <el-dialog
      v-model="editingVisible"
      title="编辑黑名单记录"
      width="520px"
      :close-on-click-modal="!editSaving"
    >
      <el-alert
        v-if="editError"
        :title="editError"
        type="error"
        show-icon
        :closable="false"
        class="blacklist-view__dialog-error"
      />
      <el-form label-position="top"
        ><FormField label="黑名单原因" required
          ><el-input
            v-model="editForm.reason"
            type="textarea"
            :rows="4"
            maxlength="500"
            show-word-limit
        /></FormField>
        <div class="blacklist-view__date-row">
          <FormField label="结束日期"
            ><el-date-picker
              v-model="editForm.endDate"
              type="date"
              value-format="YYYY-MM-DD"
              style="width: 100%" /></FormField
          ><FormField label="状态"
            ><el-select v-model="editForm.status" style="width: 100%"
              ><el-option label="生效中" value="ACTIVE" /><el-option
                label="已解除"
                value="RELEASED" /></el-select
          ></FormField></div
      ></el-form>
      <template #footer
        ><div class="blacklist-view__actions">
          <AppButton label="取消" :disabled="editSaving" @click="editing = null" /><AppButton
            label="保存"
            variant="primary"
            :loading="editSaving"
            @click="saveEdit"
          /></div
      ></template>
    </el-dialog>
    <el-dialog
      :model-value="Boolean(deleting)"
      title="删除黑名单记录"
      width="440px"
      :close-on-click-modal="!editSaving"
      @update:model-value="!$event && (deleting = null)"
      ><p>确定删除该黑名单记录吗？删除后不可恢复。</p>
      <template #footer
        ><AppButton label="取消" :disabled="editSaving" @click="deleting = null" /><AppButton
          label="确认删除"
          variant="danger"
          :loading="editSaving"
          @click="remove" /></template
    ></el-dialog>
  </section>
</template>

<style scoped>
.blacklist-view {
  display: grid;
  gap: var(--space-6);
}
.blacklist-view__toolbar {
  display: flex;
  justify-content: flex-end;
}
.blacklist-view__dialog-error {
  margin-bottom: var(--space-4);
}
.blacklist-view__selected {
  margin: var(--space-2) 0 0;
  color: var(--text-secondary);
  font-size: var(--font-size-caption);
  line-height: var(--line-height-caption);
}
.blacklist-view__date-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-4);
}
.blacklist-view__actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-2);
}
</style>
