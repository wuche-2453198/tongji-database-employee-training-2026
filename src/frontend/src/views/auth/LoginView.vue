<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import type { LoginRequest } from '@/types/auth'
import type { FormInstance, FormRules } from 'element-plus'

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()

// 测试账号仅在开发环境展示
const isDev = import.meta.env.DEV
const DEV_PASSWORD = 'Password2026!'
const devAccounts = [
  { label: 'admin', identifier: 'admin' },
  { label: 'employee', identifier: 'employee' },
  { label: 'manager', identifier: 'manager' },
  { label: 'hr', identifier: 'hr' },
]

function fillTestAccount(identifier: string) {
  form.identifier = identifier
  form.password = DEV_PASSWORD
}

const loginFormRef = ref<FormInstance>()
const form = reactive<LoginRequest>({
  identifier: '',
  password: '',
})
const loading = ref(false)
const errorMsg = ref('')
const visualFailed = ref(false)

const rules: FormRules = {
  identifier: [
    { required: true, message: '请输入账号', trigger: 'blur' },
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
  ],
}

async function handleLogin() {
  const valid = await loginFormRef.value?.validate().catch(() => false)
  if (!valid) return

  loading.value = true
  errorMsg.value = ''

  const result = await auth.login(form)
  loading.value = false

  if (!result.success) {
    errorMsg.value = result.message
    // 登录失败保留账号，清除密码
    form.password = ''
    return
  }

  // 跳转到原本要访问的页面或工作台
  const redirect = (route.query.redirect as string) || '/dashboard'
  router.push(redirect)
}
</script>

<template>
  <div class="login-page">
    <!-- 左侧品牌视觉区 -->
    <div class="login-visual">
      <img
        v-if="!visualFailed"
        class="login-visual__img"
        :src="'/images/login.jpg'"
        alt=""
        @error="visualFailed = true"
      >
      <div class="login-visual__overlay" />
      <div class="login-visual__content">
        <h2>让学习连接成长</h2>
        <p>让培训驱动组织发展</p>
      </div>
    </div>

    <!-- 右侧表单区 -->
    <div class="login-panel">
      <div class="login-panel__inner">
        <div class="login-header">
          <div class="login-logo">
            <el-icon :size="40" color="var(--color-primary)"><component :is="'Reading'" /></el-icon>
          </div>
          <h1>企业培训管理系统</h1>
          <p>员工培训 · 技能提升 · 人才发展</p>
        </div>

        <el-form
          ref="loginFormRef"
          :model="form"
          :rules="rules"
          label-position="top"
          size="large"
          class="login-form"
          @submit.prevent="handleLogin"
        >
          <el-alert
            v-if="errorMsg"
            :title="errorMsg"
            type="error"
            show-icon
            :closable="true"
            @close="errorMsg = ''"
            style="margin-bottom: 16px"
          />

          <el-form-item label="账号" prop="identifier">
            <el-input
              v-model="form.identifier"
              placeholder="员工编号 / 登录名 / 邮箱 / 手机号"
              autocomplete="username"
            />
          </el-form-item>

          <el-form-item label="密码" prop="password">
            <el-input
              v-model="form.password"
              type="password"
              placeholder="请输入密码"
              show-password
              autocomplete="current-password"
              @keyup.enter="handleLogin"
            />
          </el-form-item>

          <el-form-item>
            <el-button
              type="primary"
              :loading="loading"
              class="login-form__submit"
              @click="handleLogin"
            >
              {{ loading ? '登录中...' : '登 录' }}
            </el-button>
          </el-form-item>
        </el-form>

        <div v-if="isDev" class="login-footer">
          <div class="login-footer__hint">测试账号（点击自动填充）</div>
          <div class="login-footer__accounts">
            <button
              v-for="acc in devAccounts"
              :key="acc.identifier"
              type="button"
              class="login-footer__account"
              @click="fillTestAccount(acc.identifier)"
            >
              {{ acc.label }}
            </button>
          </div>
          <div class="login-footer__password">统一密码：{{ DEV_PASSWORD }}</div>
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.login-page {
  min-height: 100vh;
  display: grid;
  grid-template-columns: minmax(0, 58fr) minmax(460px, 42fr);
  background: var(--color-bg-card);
}

/* 左侧视觉区 */
.login-visual {
  position: relative;
  overflow: hidden;
  background: linear-gradient(160deg, #1e40af 0%, #1a56db 55%, #2563eb 100%);

  &__img {
    position: absolute;
    inset: 0;
    width: 100%;
    height: 100%;
    object-fit: cover;
  }

  &__overlay {
    position: absolute;
    inset: 0;
    background: linear-gradient(
      180deg,
      rgba(17, 24, 39, 0.12) 0%,
      rgba(17, 24, 39, 0.28) 60%,
      rgba(17, 24, 39, 0.55) 100%
    );
  }

  &__content {
    position: absolute;
    left: 64px;
    bottom: 64px;
    color: #fff;

    h2 {
      font-size: 32px;
      font-weight: 600;
      margin-bottom: 12px;
      letter-spacing: 1px;
      text-shadow: 0 2px 12px rgba(0, 0, 0, 0.35);
    }

    p {
      font-size: 16px;
      opacity: 0.92;
      letter-spacing: 2px;
      text-shadow: 0 1px 8px rgba(0, 0, 0, 0.35);
    }
  }
}

/* 右侧表单区 */
.login-panel {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 48px 24px;
  background: var(--color-bg-card);

  &__inner {
    width: 100%;
    max-width: 440px;
  }
}

.login-header {
  text-align: center;
  margin-bottom: 32px;

  .login-logo {
    margin-bottom: 12px;
  }

  h1 {
    font-size: 24px;
    font-weight: 600;
    color: var(--color-text-primary);
    margin-bottom: 8px;
  }

  p {
    font-size: 13px;
    color: var(--color-text-secondary);
  }
}

.login-form {
  :deep(.el-input__wrapper) {
    min-height: 44px;
  }

  &__submit {
    width: 100%;
    height: 44px;
  }
}

.login-footer {
  margin-top: 12px;
  padding: 12px 14px;
  background: var(--color-surface-subtle);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  text-align: center;

  &__hint {
    font-size: 12px;
    color: var(--color-text-placeholder);
    margin-bottom: 10px;
  }

  &__accounts {
    display: flex;
    flex-wrap: wrap;
    justify-content: center;
    gap: 8px;
    margin-bottom: 10px;
  }

  &__account {
    padding: 4px 12px;
    font-size: 13px;
    color: var(--color-primary);
    background: var(--color-primary-bg);
    border: 1px solid var(--color-primary-light);
    border-radius: var(--radius-sm);
    cursor: pointer;
    transition: background 0.15s, color 0.15s;

    &:hover {
      background: var(--color-primary);
      color: #fff;
    }
  }

  &__password {
    font-size: 12px;
    color: var(--color-text-secondary);
  }
}

/* 小屏：隐藏图片区，回退居中卡片 */
@media (max-width: 900px) {
  .login-page {
    grid-template-columns: 1fr;
  }

  .login-visual {
    display: none;
  }

  .login-panel {
    min-height: 100vh;
  }
}
</style>
