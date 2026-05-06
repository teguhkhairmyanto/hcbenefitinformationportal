<script setup>
// Import CButton dari CoreUI
import { CInputGroup, CFormInput, CButton } from '@coreui/vue'

const props = defineProps({
  modelValue: String,
  placeholder: String
})

// Tambahkan emit 'search'
const emit = defineEmits(['update:modelValue', 'search'])
</script>

<template>
  <div>
    <CInputGroup class="custom-search custom-search-group">
      
      <CFormInput
        :value="modelValue"
        @input="emit('update:modelValue', $event.target.value)"
        @keyup.enter="emit('search')"
        :placeholder="placeholder || 'Apa yang mau kamu tanyakan ?'"
        class="search-input-field"
      />

      <CButton 
        type="button" 
        class="btn-gemini" 
        @click="emit('search')"
        :disabled="!modelValue" 
      >
        <AppIcon name="cilSearch" class="icon-search" />
      </CButton>

    </CInputGroup>
  </div>
</template>
