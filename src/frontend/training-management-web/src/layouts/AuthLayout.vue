<template>
  <main class="auth-layout">
    <section class="auth-layout__brand" aria-labelledby="auth-brand-title">
      <div class="auth-layout__brand-copy">
        <span class="auth-layout__eyebrow">TRAINING MANAGEMENT</span>
        <h1 id="auth-brand-title">让培训过程清晰、可追踪</h1>
        <p>统一管理课程、申请、审批、报名、签到与证书，让每个角色都能专注自己的工作。</p>
      </div>
      <div class="auth-layout__geometry" aria-hidden="true">
        <span></span><span></span><span></span>
      </div>
    </section>
    <section class="auth-layout__form-panel">
      <RouterView />
    </section>
  </main>
</template>

<style scoped>
.auth-layout {
  display: grid;
  min-height: 100vh;
  grid-template-columns: minmax(420px, 1.1fr) minmax(460px, 0.9fr);
  background: var(--color-surface);
}

.auth-layout__brand {
  position: relative;
  display: flex;
  min-height: 100vh;
  align-items: center;
  overflow: hidden;
  padding: 80px;
  color: var(--text-on-primary);
  background: linear-gradient(145deg, #0f2f72, #2563eb 72%, #60a5fa);
}

.auth-layout__brand-copy {
  position: relative;
  z-index: 2;
  max-width: 560px;
}

.auth-layout__eyebrow {
  display: block;
  margin-bottom: var(--space-5);
  font-size: var(--font-size-caption);
  font-weight: 700;
  letter-spacing: 0.16em;
  opacity: 0.78;
}

.auth-layout h1 {
  margin: 0;
  font-size: 40px;
  line-height: 1.3;
}

.auth-layout p {
  max-width: 480px;
  margin: var(--space-5) 0 0;
  font-size: 16px;
  line-height: 1.9;
  opacity: 0.82;
}

.auth-layout__geometry span {
  position: absolute;
  border: 1px solid rgb(255 255 255 / 18%);
  border-radius: 50%;
}

.auth-layout__geometry span:nth-child(1) {
  width: 420px;
  height: 420px;
  right: -160px;
  top: -100px;
}
.auth-layout__geometry span:nth-child(2) {
  width: 260px;
  height: 260px;
  right: 36px;
  bottom: 48px;
}
.auth-layout__geometry span:nth-child(3) {
  width: 120px;
  height: 120px;
  left: 56px;
  bottom: 80px;
  background: rgb(255 255 255 / 5%);
}

.auth-layout__form-panel {
  display: grid;
  place-items: center;
  padding: 64px;
}
</style>
