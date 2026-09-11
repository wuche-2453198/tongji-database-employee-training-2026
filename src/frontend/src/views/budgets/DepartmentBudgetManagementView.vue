<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import AppButton from '@/components/common/AppButton.vue'
import AppPagination from '@/components/common/AppPagination.vue'
import ConfirmDialog from '@/components/common/ConfirmDialog.vue'
import DataTable from '@/components/common/DataTable.vue'
import FormField from '@/components/common/FormField.vue'
import PageHeader from '@/components/common/PageHeader.vue'
import PageState from '@/components/common/PageState.vue'
import SearchPanel from '@/components/common/SearchPanel.vue'
import { hasAnyRole } from '@/config/permissions'
import { getDepartmentBudgetService } from '@/services/budget'
import { useAuthStore } from '@/stores/auth'
import { isServiceError, type UiError } from '@/types/api'
import type { DepartmentBudget } from '@/domains/budget'
import type { TableColumn } from '@/types/ui'

const authStore = useAuthStore()
const canManage = computed(() => hasAnyRole(authStore.currentUser, ['ADMIN']))
const filters = reactive({
  keyword: '',
  minBudget: undefined as number | undefined,
  maxBudget: undefined as number | undefined,
  page: 1,
  pageSize: 20,
})
const records = ref<DepartmentBudget[]>([])
const total = ref(0)
const loading = ref(false)
const loaded = ref(false)
const error = ref<UiError | null>(null)
const editing = ref<DepartmentBudget | null>(null)
const creating = ref(false)
const saving = ref(false)
const formError = ref('')
const deleting = ref<DepartmentBudget | null>(null)
const form = reactive({ departmentName: '', annualBudget: 0 })
const columns: TableColumn[] = [
  { key: 'departmentName', label: '部门', minWidth: 160 },
  { key: 'annualBudget', label: '年度总预算', width: 150, align: 'right' },
  { key: 'usedBudget', label: '已使用', width: 140, align: 'right' },
  { key: 'remainingBudget', label: '剩余预算', width: 150, align: 'right' },
  { key: 'usage', label: '使用率', width: 130 },
]
const state = computed(() =>
  loading.value && !loaded.value
    ? 'loading'
    : error.value
      ? 'error'
      : records.value.length
        ? 'default'
        : 'empty',
)
const dialogVisible = computed({
  get: () => creating.value || editing.value !== null,
  set: (value: boolean) => {
    if (!value && !saving.value) {
      creating.value = false
      editing.value = null
    }
  },
})
const dialogTitle = computed(() => (creating.value ? '新增部门预算' : '编辑部门预算'))
const money = (value: unknown) =>
  new Intl.NumberFormat('zh-CN', {
    style: 'currency',
    currency: 'CNY',
    maximumFractionDigits: 2,
  }).format(Number(value) || 0)
const usage = (record: DepartmentBudget) =>
  record.annualBudget > 0 ? `${((record.usedBudget / record.annualBudget) * 100).toFixed(1)}%` : '—'
async function load() {
  loading.value = true
  error.value = null
  try {
    const result = await (
      await getDepartmentBudgetService()
    ).list({
      keyword: filters.keyword || undefined,
      minBudget: filters.minBudget,
      maxBudget: filters.maxBudget,
      page: filters.page,
      pageSize: filters.pageSize,
    })
    records.value = result.items
    total.value = result.total
    loaded.value = true
  } catch (caught) {
    error.value = isServiceError(caught)
      ? caught.ui
      : {
          kind: 'unknown',
          code: 'UNKNOWN',
          message: '部门预算加载失败。',
          fieldErrors: [],
          retryable: false,
          resultUnknown: false,
        }
    loaded.value = true
  } finally {
    loading.value = false
  }
}
async function sync(next: Partial<typeof filters>, reset = false) {
  Object.assign(filters, next)
  if (reset) filters.page = 1
  await load()
}
function openCreate() {
  formError.value = ''
  form.departmentName = ''
  form.annualBudget = 0
  creating.value = true
}
function openEdit(record: DepartmentBudget) {
  formError.value = ''
  form.departmentName = record.departmentName
  form.annualBudget = record.annualBudget
  editing.value = record
}
async function save() {
  if (!form.departmentName.trim()) {
    formError.value = '请填写部门名称。'
    return
  }
  if (form.annualBudget < 0) {
    formError.value = '年度预算不能为负数。'
    return
  }
  saving.value = true
  formError.value = ''
  try {
    const service = await getDepartmentBudgetService()
    if (creating.value)
      await service.create({
        departmentName: form.departmentName.trim(),
        annualBudget: form.annualBudget,
      })
    else if (editing.value)
      await service.update(editing.value.id, {
        departmentName: form.departmentName.trim(),
        annualBudget: form.annualBudget,
      })
    creating.value = false
    editing.value = null
    await load()
  } catch (caught) {
    formError.value = isServiceError(caught) ? caught.ui.message : '保存部门预算失败。'
  } finally {
    saving.value = false
  }
}
async function remove() {
  if (!deleting.value) return
  saving.value = true
  try {
    await (await getDepartmentBudgetService()).delete(deleting.value.id)
    deleting.value = null
    await load()
  } catch (caught) {
    error.value = isServiceError(caught) ? caught.ui : error.value
  } finally {
    saving.value = false
  }
}
onMounted(load)
</script>

<template>
  <section class="budget-management">
    <PageHeader
      title="部门预算管理"
      description="维护部门年度培训预算，课程发布时将自动占用对应部门余额。"
      ><template #action
        ><AppButton
          v-if="canManage"
          label="新增预算"
          variant="primary"
          @click="openCreate" /></template></PageHeader
    ><SearchPanel
      :expanded="true"
      :searching="loading"
      @search="sync({}, true)"
      @reset="sync({ keyword: '', minBudget: undefined, maxBudget: undefined, page: 1 }, true)"
      ><el-input
        v-model="filters.keyword"
        clearable
        placeholder="部门名称"
        aria-label="部门关键词" /><el-input-number
        v-model="filters.minBudget"
        :min="0"
        placeholder="最小年度预算"
        style="width: 100%" /><el-input-number
        v-model="filters.maxBudget"
        :min="0"
        placeholder="最大年度预算"
        style="width: 100%" /></SearchPanel
    ><PageState v-if="state === 'loading'" state="loading" /><PageState
      v-else-if="error"
      state="error"
      :description="error.message"
      primary-label="重新加载"
      @primary="load"
    /><PageState
      v-else-if="state === 'empty'"
      state="empty"
      title="暂无部门预算"
      compact
    /><template v-else
      ><DataTable :columns="columns" :rows="records" :has-actions="canManage"
        ><template #cell="{ column, row }"
          ><span v-if="['annualBudget', 'usedBudget', 'remainingBudget'].includes(column.key)">{{
            money(row[column.key])
          }}</span
          ><span v-else-if="column.key === 'usage'">{{ usage(row as DepartmentBudget) }}</span
          ><span v-else>{{ row[column.key] || '—' }}</span></template
        ><template #actions="{ row }"
          ><AppButton
            label="编辑"
            variant="text"
            @click="openEdit(row as DepartmentBudget)" /><AppButton
            label="删除"
            variant="text"
            @click="deleting = row as DepartmentBudget" /></template></DataTable
      ><AppPagination
        v-if="total"
        :page="filters.page"
        :page-size="filters.pageSize"
        :total="total"
        :disabled="loading"
        @update:page="sync({ page: $event })"
        @update:page-size="sync({ pageSize: $event }, true)" /></template
    ><el-dialog
      v-model="dialogVisible"
      :title="dialogTitle"
      width="480px"
      :close-on-click-modal="!saving"
      ><el-alert
        v-if="formError"
        :title="formError"
        type="error"
        show-icon
        :closable="false" /><el-form label-position="top"
        ><FormField label="部门名称" required
          ><el-input v-model="form.departmentName" maxlength="100" /></FormField
        ><FormField label="年度培训预算" required
          ><el-input-number
            v-model="form.annualBudget"
            :min="0"
            :precision="2"
            style="width: 100%" /></FormField></el-form
      ><template #footer
        ><AppButton label="取消" :disabled="saving" @click="dialogVisible = false" /><AppButton
          label="保存"
          variant="primary"
          :loading="saving"
          @click="save" /></template></el-dialog
    ><ConfirmDialog
      :model-value="Boolean(deleting)"
      title="删除部门预算"
      :description="`确定删除 ${deleting?.departmentName ?? ''} 的预算记录吗？`"
      confirm-label="确认删除"
      type="danger"
      :loading="saving"
      @update:model-value="!$event && (deleting = null)"
      @confirm="remove"
    />
  </section>
</template>

<style scoped>
.budget-management {
  display: grid;
  gap: var(--space-6);
}
</style>
