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
import { getRatingService } from '@/services/rating'
import { isServiceError, type UiError } from '@/types/api'
import type { CourseRating } from '@/domains/rating'
import type { TableColumn } from '@/types/ui'

const filters = reactive({ courseId: '', trainerId: '', page: 1, pageSize: 20 })
const records = ref<CourseRating[]>([])
const total = ref(0)
const loading = ref(false)
const loaded = ref(false)
const error = ref<UiError | null>(null)
const verifying = ref<CourseRating | null>(null)
const comment = ref('')
const saving = ref(false)
const formError = ref('')
const verifyVisible = computed({
  get: () => verifying.value !== null,
  set: (value: boolean) => {
    if (!value && !saving.value) verifying.value = null
  },
})
const columns: TableColumn[] = [
  { key: 'courseName', label: '课程', minWidth: 180 },
  { key: 'trainerName', label: '讲师', width: 120 },
  { key: 'employeeName', label: '员工', width: 110 },
  { key: 'score', label: '评分', width: 130 },
  { key: 'comment', label: '评价', minWidth: 200 },
  { key: 'hrVerified', label: '复核', width: 100, align: 'center' },
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
async function load() {
  loading.value = true
  error.value = null
  try {
    const result = await (
      await getRatingService()
    ).listAll({
      courseId: filters.courseId || undefined,
      trainerId: filters.trainerId || undefined,
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
          message: '评分记录加载失败。',
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
function openVerify(record: CourseRating) {
  verifying.value = record
  comment.value = ''
  formError.value = ''
}
async function verify() {
  if (!verifying.value) return
  saving.value = true
  formError.value = ''
  try {
    await (await getRatingService()).verify(verifying.value.id, comment.value)
    verifying.value = null
    await load()
  } catch (caught) {
    formError.value = isServiceError(caught) ? caught.ui.message : '复核评分失败。'
  } finally {
    saving.value = false
  }
}
onMounted(load)
</script>

<template>
  <section class="rating-management">
    <PageHeader title="讲师评分管理" description="查看员工评价，并完成 HR 复核。" /><SearchPanel
      :expanded="false"
      :searching="loading"
      @search="sync({}, true)"
      @reset="sync({ courseId: '', trainerId: '', page: 1 }, true)"
      ><el-input
        v-model="filters.courseId"
        clearable
        inputmode="numeric"
        placeholder="课程编号"
        aria-label="课程编号" /><el-input
        v-model="filters.trainerId"
        clearable
        inputmode="numeric"
        placeholder="讲师编号"
        aria-label="讲师编号" /></SearchPanel
    ><PageState v-if="state === 'loading'" state="loading" /><PageState
      v-else-if="error"
      state="error"
      :description="error.message"
      primary-label="重新加载"
      @primary="load"
    /><PageState
      v-else-if="state === 'empty'"
      state="empty"
      title="暂无评分记录"
      compact
    /><template v-else
      ><DataTable :columns="columns" :rows="records" has-actions
        ><template #cell="{ column, row }"
          ><span v-if="column.key === 'score'">{{ Number(row.score).toFixed(1) }} / 5</span
          ><StatusTag
            v-else-if="column.key === 'hrVerified'"
            :label="row.hrVerified === 'Y' ? '已复核' : '未复核'"
            :semantic="row.hrVerified === 'Y' ? 'success' : 'warning'"
          /><span v-else>{{ row[column.key] || '—' }}</span></template
        ><template #actions="{ row }"
          ><AppButton
            v-if="row.hrVerified !== 'Y'"
            label="复核"
            variant="text"
            @click="openVerify(row as CourseRating)" /></template></DataTable
      ><AppPagination
        v-if="total"
        :page="filters.page"
        :page-size="filters.pageSize"
        :total="total"
        :disabled="loading"
        @update:page="sync({ page: $event })"
        @update:page-size="sync({ pageSize: $event }, true)" /></template
    ><el-dialog
      v-model="verifyVisible"
      title="复核讲师评分"
      width="480px"
      :close-on-click-modal="!saving"
      ><el-alert v-if="formError" :title="formError" type="error" :closable="false" show-icon />
      <p>
        {{ verifying?.employeeName }} 对《{{ verifying?.courseName }}》评分 {{ verifying?.score }} /
        5。
      </p>
      <FormField label="复核意见"
        ><el-input
          v-model="comment"
          type="textarea"
          :rows="3"
          maxlength="500"
          show-word-limit /></FormField
      ><template #footer
        ><AppButton label="取消" :disabled="saving" @click="verifying = null" /><AppButton
          label="确认复核"
          variant="primary"
          :loading="saving"
          @click="verify" /></template
    ></el-dialog>
  </section>
</template>

<style scoped>
.rating-management {
  display: grid;
  gap: var(--space-6);
}
</style>
