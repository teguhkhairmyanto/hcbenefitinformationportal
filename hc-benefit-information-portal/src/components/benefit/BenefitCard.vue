<template>
  <CCard class="mb-3">
    <CCardBody>
      <h5>{{ title }}</h5>
      <p>{{ description }}</p>
    </CCardBody>
  </CCard>
</template>

<script>
import { CCard, CCardBody } from '@coreui/vue'

export default {
  props: ['title', 'description'],
  components: { CCard, CCardBody }
}
</script>