<script setup lang="ts">
import { onMounted, reactive, watch, ref } from 'vue'
import AppButton from '@/components/common/AppButton.vue'
import FormField from '@/components/common/FormField.vue'
import { getCourseService } from '@/services/course'
import { getTrainerService } from '@/services/trainer'
import { isServiceError } from '@/types/api'
import type { CourseDetail, CourseEditorInput } from '@/domains/course'
import type { Trainer } from '@/domains/trainer'

const props = defineProps<{ modelValue: boolean; course?: CourseDetail | null }>()
const emit = defineEmits<{ 'update:modelValue': [value: boolean]; saved: [id: string] }>()
const saving = ref(false)
const error = ref('')
const trainers = ref<Trainer[]>([])
const loadingTrainers = ref(false)
const form = reactive<CourseEditorInput>({
  name: '',
  type: '技术培训',
  durationHours: 1,
  trainerId: '',
  maxStudents: 20,
  startAt: '',
  endAt: '',
  location: '',
  budgetAmount: 0,
  deptId: '',
  preTestUrl: '',
  postTestUrl: '',
  materialUrl: '',
})
function reset(course?: CourseDetail | null) {
  error.value = ''
  form.name = course?.name ?? ''
  form.type = (course?.type === 'UNKNOWN' ? '技术培训' : course?.type) ?? '技术培训'
  form.durationHours = course?.hours ?? 1
  form.trainerId = course?.trainerId ?? course?.trainer.id ?? ''
  form.maxStudents = course?.maxStudents ?? 20
  form.startAt = course?.startTime?.slice(0, 16) ?? ''
  form.endAt = course?.endTime?.slice(0, 16) ?? ''
  form.location = course?.location === '—' ? '' : (course?.location ?? '')
  form.budgetAmount = course?.budgetAmount ?? 0
  form.deptId = course?.deptId ?? ''
  form.preTestUrl = course?.preTestUrl ?? ''
  form.postTestUrl = course?.postTestUrl ?? ''
  form.materialUrl = course?.materialUrl ?? ''
}
watch(
  () => [props.modelValue, props.course] as const,
  ([visible, course]) => {
    if (visible) reset(course)
  },
  { immediate: true },
)
async function loadTrainers() {
  loadingTrainers.value = true
  try {
    trainers.value = (await (await getTrainerService()).list({ page: 1, pageSize: 100 })).items
  } finally {
    loadingTrainers.value = false
  }
}
function validate() {
  if (
    !form.name.trim() ||
    !form.trainerId ||
    !form.deptId ||
    !form.startAt ||
    !form.endAt ||
    !form.location.trim()
  )
    return '请完成课程名称、讲师、主办部门、时间和地点。'
  if (form.durationHours <= 0 || form.maxStudents <= 0 || form.budgetAmount < 0)
    return '学时、最大人数和预算金额不符合要求。'
  if (new Date(form.endAt) <= new Date(form.startAt)) return '结束时间必须晚于开始时间。'
  return ''
}
async function save() {
  const message = validate()
  if (message) {
    error.value = message
    return
  }
  saving.value = true
  error.value = ''
  try {
    const service = await getCourseService()
    const payload = { ...form, name: form.name.trim(), location: form.location.trim() }
    const result = props.course
      ? await service.updateCourse(props.course.id, payload)
      : await service.createCourse(payload)
    emit('update:modelValue', false)
    emit('saved', result.id)
  } catch (caught) {
    error.value = isServiceError(caught) ? caught.ui.message : '课程保存失败。'
  } finally {
    saving.value = false
  }
}
onMounted(loadTrainers)
</script>

<template>
  <el-dialog
    :model-value="modelValue"
    :title="course ? '编辑课程' : '新建课程'"
    width="680px"
    :close-on-click-modal="!saving"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <el-alert v-if="error" :title="error" type="error" show-icon :closable="false" />
    <el-form label-position="top"
      ><FormField label="课程名称" required
        ><el-input v-model="form.name" maxlength="100"
      /></FormField>
      <div class="grid">
        <FormField label="课程类型" required
          ><el-select v-model="form.type" style="width: 100%"
            ><el-option
              v-for="type in ['技术培训', '管理培训', '产品培训', '营销培训']"
              :key="type"
              :label="type"
              :value="type" /></el-select></FormField
        ><FormField label="学时" required
          ><el-input-number
            v-model="form.durationHours"
            :min="0.1"
            :max="999.9"
            :step="0.5"
            style="width: 100%"
        /></FormField>
      </div>
      <div class="grid">
        <FormField label="讲师" required
          ><el-select
            v-model="form.trainerId"
            filterable
            :loading="loadingTrainers"
            placeholder="请选择讲师"
            style="width: 100%"
            ><el-option
              v-for="trainer in trainers"
              :key="trainer.id"
              :label="`${trainer.name} · ${trainer.company || '—'}（${trainer.starLevel.toFixed(1)} 星）`"
              :value="trainer.id" /></el-select></FormField
        ><FormField label="主办部门 ID" required
          ><el-input v-model="form.deptId" inputmode="numeric" placeholder="请输入部门编号"
        /></FormField>
      </div>
      <div class="grid">
        <FormField label="最大人数" required
          ><el-input-number v-model="form.maxStudents" :min="1" style="width: 100%" /></FormField
        ><FormField label="预算金额" required
          ><el-input-number v-model="form.budgetAmount" :min="0" :precision="2" style="width: 100%"
        /></FormField>
      </div>
      <div class="grid">
        <FormField label="开始时间" required
          ><el-date-picker
            v-model="form.startAt"
            type="datetime"
            value-format="YYYY-MM-DDTHH:mm:ss"
            style="width: 100%" /></FormField
        ><FormField label="结束时间" required
          ><el-date-picker
            v-model="form.endAt"
            type="datetime"
            value-format="YYYY-MM-DDTHH:mm:ss"
            style="width: 100%"
        /></FormField>
      </div>
      <FormField label="培训地点" required
        ><el-input v-model="form.location" maxlength="100" /></FormField
      ><FormField label="课前测试链接"><el-input v-model="form.preTestUrl" /></FormField
      ><FormField label="课后测试链接"><el-input v-model="form.postTestUrl" /></FormField
      ><FormField label="课程资料链接"><el-input v-model="form.materialUrl" /></FormField
    ></el-form>
    <template #footer
      ><AppButton
        label="取消"
        :disabled="saving"
        @click="emit('update:modelValue', false)" /><AppButton
        label="保存草稿"
        variant="primary"
        :loading="saving"
        @click="save"
    /></template>
  </el-dialog>
</template>

<style scoped>
.grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-4);
}
</style>
