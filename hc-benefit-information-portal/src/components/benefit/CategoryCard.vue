<template>
  <div class="category-card shadow-sm">
    <div class="card-icon-wrapper">
      <AppIcon :name="icon" class="category-icon" />
    </div>
    
    <div class="category-title">
      {{ title }}
    </div>
  </div>
</template>

<script>
import AppIcon from '@/components/common/AppIcon.vue'

export default {
  name: 'CategoryCard',
  props: {
    title: {
      type: String,
      required: true
    },
    icon: {
      type: String,
      required: true
    }
  },
  components: { AppIcon }
}
</script>