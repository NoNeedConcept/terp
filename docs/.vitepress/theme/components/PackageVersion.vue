<script setup lang="ts">
import { computed } from 'vue'
import { data } from '../../data/versions.data'

const props = defineProps<{ package: string; bare?: boolean }>()

const version = computed(() => data[props.package])
const unreleased = computed(() => !version.value || version.value === '0.0.0')
</script>

<template>
  <span class="pkg-version" :class="{ 'is-unreleased': unreleased }"><span v-if="!bare" class="pkg-name">{{ package }} </span><span class="pkg-ver">{{ unreleased ? 'unreleased' : 'v' + version }}</span></span>
</template>
