<script setup>
import * as icons from '@coreui/icons'
import { computed } from 'vue'
import { CIcon } from '@coreui/icons-vue'

const props = defineProps({
  name: String
})

// ambil icon berdasarkan nama string
const icon = computed(() => icons[props.name])
</script>

<template>
  <CIcon :icon="icon" />
</template>