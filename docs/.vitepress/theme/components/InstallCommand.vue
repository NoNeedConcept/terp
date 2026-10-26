<script setup lang="ts">
import { ref } from 'vue'

const props = defineProps<{ cmd: string }>()
const state = ref<'idle' | 'copied' | 'failed'>('idle')

async function copy() {
  try {
    await navigator.clipboard.writeText(props.cmd)
    state.value = 'copied'
  } catch {
    state.value = 'failed'
  }
  setTimeout(() => (state.value = 'idle'), 1800)
}
</script>

<template>
  <span class="install">
    <code class="install-cmd">{{ cmd }}</code>
    <button class="install-copy" type="button" :aria-label="'Copy: ' + cmd" @click="copy">
      {{ state === 'copied' ? 'Copied' : state === 'failed' ? 'Select and copy' : 'Copy' }}
    </button>
  </span>
</template>
