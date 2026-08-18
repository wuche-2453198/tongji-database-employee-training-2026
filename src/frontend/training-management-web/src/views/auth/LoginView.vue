<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Lock, User } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'

import { useAuthStore } from '@/stores/auth'
import { isAuthServiceError } from '@/types/auth'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const formRef = ref<FormInstance>()
const formAlert = ref<{ message: string; type: 'error' | 'warning' } | null>(null)
const form = reactive({ account: '', password: '' })
const submitting = computed(() => authStore.status === 'authenticating')
const sessionExpired = computed(() => route.query.reason === 'session-expired')

const rules: FormRules<typeof form> = {
  account: [{ required: true, message: '请输入员工账号', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }],
}

const safeRedirect = () => {
  const candidate = typeof route.query.redirect === 'string' ? route.query.redirect : ''
  return candidate.startsWith('/') && !candidate.startsWith('//') && !candidate.startsWith('/login')
    ? candidate
    : '/dashboard'
}

const submit = async () => {
  formAlert.value = null
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return

  try {
    await authStore.login(form)
    await router.replace(safeRedirect())
  } catch (error) {
    const code = isAuthServiceError(error) ? error.code : 'SERVICE_UNAVAILABLE'
    const alertByCode = {
      INVALID_CREDENTIALS: { message: '账号或密码错误，请重新输入', type: 'error' },
      ACCOUNT_DISABLED: { message: '当前账号不可用，请联系管理员', type: 'warning' },
      SERVICE_UNAVAILABLE: { message: '登录服务暂时不可用，请稍后重试', type: 'error' },
      AUTH_NOT_CONFIGURED: { message: '真实认证服务尚未接入，当前环境无法登录', type: 'warning' },
      SESSION_EXPIRED: { message: '登录状态已过期，请重新登录', type: 'warning' },
    } as const

    formAlert.value = alertByCode[code]
    form.password = ''
  }
}
</script>

<template>
  <div class="login-view">
    <div class="login-view__heading">
      <span class="login-view__mark">T</span>
      <div>
        <h2>登录系统</h2>
        <p>使用培训管理账号继续</p>
      </div>
    </div>

    <el-alert
      v-if="formAlert || sessionExpired"
      :title="formAlert?.message ?? '登录状态已过期，请重新登录'"
      :type="formAlert?.type ?? 'warning'"
      show-icon
      :closable="false"
      class="login-view__alert"
    />

    <el-form
      ref="formRef"
      :model="form"
      :rules="rules"
      label-position="top"
      size="large"
      @submit.prevent="submit"
    >
      <el-form-item label="员工账号" prop="account">
        <el-input
          v-model="form.account"
          autocomplete="username"
          placeholder="请输入员工账号"
          :disabled="submitting"
          :prefix-icon="User"
        />
      </el-form-item>
      <el-form-item label="密码" prop="password">
        <el-input
          v-model="form.password"
          type="password"
          autocomplete="current-password"
          placeholder="请输入密码"
          show-password
          :disabled="submitting"
          :prefix-icon="Lock"
          @keyup.enter="submit"
        />
      </el-form-item>
      <el-button
        class="login-view__submit"
        type="primary"
        size="large"
        native-type="submit"
        :loading="submitting"
      >
        {{ submitting ? '登录中…' : '登录系统' }}
      </el-button>
    </el-form>

    <p class="login-view__notice">当前为开发阶段 Mock 会话，不连接真实认证服务。</p>
  </div>
</template>

<style scoped>
.login-view {
  width: min(100%, 400px);
}

.login-view__heading {
  display: flex;
  align-items: center;
  gap: var(--space-4);
  margin-bottom: var(--space-8);
}

.login-view__mark {
  display: grid;
  width: 48px;
  height: 48px;
  place-items: center;
  border-radius: var(--radius-lg);
  color: var(--text-on-primary);
  background: var(--color-primary);
  font-size: 20px;
  font-weight: 700;
}

.login-view h2 {
  margin: 0;
  font-size: var(--font-size-title-page);
  line-height: var(--line-height-title-page);
}

.login-view__heading p,
.login-view__notice {
  color: var(--text-secondary);
}

.login-view__heading p {
  margin: var(--space-1) 0 0;
}

.login-view__alert {
  margin-bottom: var(--space-5);
}

.login-view__submit {
  width: 100%;
  margin-top: var(--space-2);
}

.login-view__notice {
  margin: var(--space-6) 0 0;
  font-size: var(--font-size-caption);
  text-align: center;
}
</style>
