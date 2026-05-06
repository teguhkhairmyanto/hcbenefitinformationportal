<script setup>
import { useRoute } from 'vue-router'

const route = useRoute()
</script>

<template>
  <div class="container mt-5">
    <h2>Category: {{ route.params.type }}</h2>
  </div>
</template>