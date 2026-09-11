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
import StatusTag from '@/components/common/StatusTag.vue'
import { getEmployeeService } from '@/services/employee'
import { isServiceError, type UiError } from '@/types/api'
import type { EmployeeSummary } from '@/domains/employee'
import type { TableColumn } from '@/types/ui'

const filters = reactive({ keyword: '', deptName: '', status: '' as '' | 'ACTIVE' | 'RESIGNED', page: 1, pageSize: 20 })
const records = ref<EmployeeSummary[]>([])
const total = ref(0)
const loading = ref(false)
const error = ref<UiError | null>(null)
const loaded = ref(false)
const dialog = ref<'create' | 'edit' | null>(null)
const selected = ref<EmployeeSummary | null>(null)
const saving = ref(false)
const formError = ref('')
const deleting = ref<EmployeeSummary | null>(null)
const form = reactive({ loginName: '', empName: '', deptName: '', position: '', email: '', phone: '', hireDate: '', password: '', status: 'ACTIVE' as 'ACTIVE' | 'RESIGNED' })
const columns: TableColumn[] = [
  { key: 'empId', label: '工号', width: 90 }, { key: 'empName', label: '姓名', width: 110 },
  { key: 'deptName', label: '部门', width: 140 }, { key: 'position', label: '职位', width: 140 },
  { key: 'email', label: '邮箱', minWidth: 180 }, { key: 'status', label: '状态', width: 100, align: 'center' },
]
const state = computed(() => loading.value && !loaded.value ? 'loading' : error.value ? 'error' : records.value.length ? 'default' : 'empty')
const dialogTitle = computed(() => dialog.value === 'create' ? '新增员工' : '编辑员工')
const dialogVisible = computed({ get: () => dialog.value !== null, set: (value: boolean) => { if (!value && !saving.value) dialog.value = null } })

function resetForm(employee?: EmployeeSummary) {
  formError.value = ''
  form.loginName = ''
  form.password = ''
  form.empName = employee?.empName ?? ''
  form.deptName = employee?.deptName ?? ''
  form.position = employee?.position === '—' ? '' : employee?.position ?? ''
  form.email = employee?.email ?? ''
  form.phone = employee?.phone ?? ''
  form.hireDate = employee?.hireDate?.slice(0, 10) ?? ''
  form.status = employee?.status ?? 'ACTIVE'
}
async function load() {
  loading.value = true; error.value = null
  try {
    const result = await (await getEmployeeService()).listEmployees({ ...filters, keyword: filters.keyword || undefined, deptName: filters.deptName || undefined, status: filters.status || undefined })
    records.value = result.items; total.value = result.total; loaded.value = true
  } catch (caught) { error.value = isServiceError(caught) ? caught.ui : { kind: 'unknown', code: 'UNKNOWN', message: '员工数据加载失败。', fieldErrors: [], retryable: false, resultUnknown: false }; loaded.value = true } finally { loading.value = false }
}
async function sync(next: Partial<typeof filters>, reset = false) { Object.assign(filters, next); if (reset) filters.page = 1; await load() }
function openCreate() { selected.value = null; resetForm(); dialog.value = 'create' }
function openEdit(employee: EmployeeSummary) { selected.value = employee; resetForm(employee); dialog.value = 'edit' }
function validate() {
  if (!form.empName.trim() || !form.deptName.trim()) return '请填写员工姓名和所属部门。'
  if (dialog.value === 'create' && (!form.loginName.trim() || !form.password)) return '新增员工时必须填写登录名和初始密码。'
  if (dialog.value === 'create' && !form.hireDate) return '请填写入职日期。'
  return ''
}
async function save() {
  const message = validate(); if (message) { formError.value = message; return }
  saving.value = true; formError.value = ''
  try {
    const service = await getEmployeeService()
    if (dialog.value === 'create') await service.createEmployee({ loginName: form.loginName.trim(), password: form.password, empName: form.empName.trim(), deptName: form.deptName.trim(), position: form.position, email: form.email, phone: form.phone, hireDate: form.hireDate })
    else if (selected.value) await service.updateEmployee(String(selected.value.empId), { empName: form.empName.trim(), deptName: form.deptName.trim(), position: form.position, email: form.email, phone: form.phone, hireDate: form.hireDate || undefined, status: form.status })
    dialog.value = null; await load()
  } catch (caught) { formError.value = isServiceError(caught) ? caught.ui.message : '保存员工信息失败。' } finally { saving.value = false }
}
async function remove() {
  if (!deleting.value) return
  saving.value = true
  try { await (await getEmployeeService()).deleteEmployee(String(deleting.value.empId)); deleting.value = null; await load() }
  catch (caught) { error.value = isServiceError(caught) ? caught.ui : error.value }
  finally { saving.value = false }
}
onMounted(load)
</script>

<template>
  <section class="employee-management">
    <PageHeader title="员工管理" description="维护员工基础资料、账号状态与组织归属。"><template #action><AppButton label="新增员工" variant="primary" @click="openCreate" /></template></PageHeader>
    <SearchPanel :expanded="true" :searching="loading" @search="sync({}, true)" @reset="sync({ keyword: '', deptName: '', status: '', page: 1 }, true)">
      <el-input v-model="filters.keyword" clearable placeholder="姓名或工号" aria-label="员工关键词" />
      <el-input v-model="filters.deptName" clearable placeholder="部门名称" aria-label="部门名称" />
      <el-select v-model="filters.status" clearable placeholder="员工状态" aria-label="员工状态"><el-option label="在职" value="ACTIVE" /><el-option label="离职" value="RESIGNED" /></el-select>
    </SearchPanel>
    <PageState v-if="state === 'loading'" state="loading" />
    <PageState v-else-if="error" state="error" :description="error.message" primary-label="重新加载" @primary="load" />
    <PageState v-else-if="state === 'empty'" state="empty" title="暂无员工记录" compact />
    <template v-else>
      <DataTable :columns="columns" :rows="records" has-actions><template #cell="{ column, row }"><StatusTag v-if="column.key === 'status'" :label="row.status === 'ACTIVE' ? '在职' : '离职'" :semantic="row.status === 'ACTIVE' ? 'success' : 'neutral'" /><span v-else>{{ row[column.key] || '—' }}</span></template><template #actions="{ row }"><AppButton label="编辑" variant="text" @click="openEdit(row as EmployeeSummary)" /><AppButton v-if="row.status === 'ACTIVE'" label="停用" variant="text" @click="deleting = row as EmployeeSummary" /></template></DataTable>
      <AppPagination v-if="total" :page="filters.page" :page-size="filters.pageSize" :total="total" :disabled="loading" @update:page="sync({ page: $event })" @update:page-size="sync({ pageSize: $event }, true)" />
    </template>
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="560px" :close-on-click-modal="!saving"><el-alert v-if="formError" :title="formError" type="error" :closable="false" show-icon /><el-form label-position="top"><FormField v-if="dialog === 'create'" label="登录名" required><el-input v-model="form.loginName" /></FormField><FormField label="员工姓名" required><el-input v-model="form.empName" /></FormField><FormField label="所属部门" required><el-input v-model="form.deptName" /></FormField><div class="grid"><FormField label="职位"><el-input v-model="form.position" /></FormField><FormField label="入职日期" :required="dialog === 'create'"><el-date-picker v-model="form.hireDate" type="date" value-format="YYYY-MM-DD" style="width:100%" /></FormField></div><div class="grid"><FormField label="邮箱"><el-input v-model="form.email" /></FormField><FormField label="电话"><el-input v-model="form.phone" /></FormField></div><FormField v-if="dialog === 'create'" label="初始密码" required><el-input v-model="form.password" type="password" show-password /></FormField><FormField v-else label="账号状态"><el-select v-model="form.status" style="width:100%"><el-option label="在职" value="ACTIVE" /><el-option label="离职" value="RESIGNED" /></el-select></FormField></el-form><template #footer><AppButton label="取消" :disabled="saving" @click="dialog = null" /><AppButton label="保存" variant="primary" :loading="saving" @click="save" /></template></el-dialog>
    <ConfirmDialog :model-value="Boolean(deleting)" title="停用员工" :description="`确定将 ${deleting?.empName ?? ''} 标记为离职吗？该操作会阻止其继续使用系统。`" confirm-label="确认停用" type="danger" :loading="saving" @update:model-value="!$event && (deleting = null)" @confirm="remove" />
  </section>
</template>

<style scoped>.employee-management{display:grid;gap:var(--space-6)}.grid{display:grid;grid-template-columns:1fr 1fr;gap:var(--space-4)}</style>
