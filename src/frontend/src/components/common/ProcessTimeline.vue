<script setup lang="ts">
import type { TimelineNode } from '@/types/ui'

defineProps<{
  nodes: TimelineNode[]
  accessibleLabel: string
}>()
</script>

<template>
  <ol class="process-timeline" :aria-label="accessibleLabel">
    <li
      v-for="(node, index) in nodes"
      :key="`${node.title}-${index}`"
      class="process-timeline__node"
      :class="`process-timeline__node--${node.state}`"
      :aria-current="node.state === 'current' ? 'step' : undefined"
    >
      <div class="process-timeline__track" aria-hidden="true">
        <span class="process-timeline__marker">
          {{ node.state === 'complete' ? '✓' : node.state === 'error' ? '!' : '' }}
        </span>
      </div>
      <div class="process-timeline__copy">
        <strong>{{ node.title }}</strong>
        <span v-if="node.meta">{{ node.meta }}</span>
        <p v-if="node.summary">{{ node.summary }}</p>
      </div>
    </li>
  </ol>
</template>

<style scoped>
.process-timeline {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  padding: 0;
  margin: 0;
  list-style: none;
}

.process-timeline__node {
  position: relative;
  min-width: 0;
}

.process-timeline__track {
  position: relative;
  display: flex;
  align-items: center;
}

.process-timeline__track::after {
  width: 100%;
  height: 2px;
  background: var(--color-border);
  content: '';
}

.process-timeline__node:last-child .process-timeline__track::after {
  visibility: hidden;
}

.process-timeline__marker {
  display: grid;
  width: 24px;
  height: 24px;
  flex: 0 0 24px;
  place-items: center;
  border: 2px solid var(--color-border-strong);
  border-radius: 50%;
  color: var(--text-disabled);
  background: var(--color-surface);
  font-size: var(--font-size-caption);
  font-weight: 700;
}

.process-timeline__node--complete .process-timeline__marker {
  border-color: var(--color-success);
  color: var(--text-on-primary);
  background: var(--color-success);
}

.process-timeline__node--complete .process-timeline__track::after {
  background: var(--color-success);
}

.process-timeline__node--current .process-timeline__marker {
  border: 6px solid var(--color-primary);
}

.process-timeline__node--error .process-timeline__marker {
  border-color: var(--color-error);
  color: var(--text-on-primary);
  background: var(--color-error);
}

.process-timeline__copy {
  display: grid;
  gap: var(--space-1);
  padding: var(--space-3) var(--space-4) 0 0;
}

.process-timeline__copy strong {
  color: var(--text-primary);
  line-height: var(--line-height-body);
}

.process-timeline__copy span,
.process-timeline__copy p {
  margin: 0;
  color: var(--text-secondary);
  font-size: var(--font-size-caption);
  line-height: var(--line-height-caption);
}
</style>
