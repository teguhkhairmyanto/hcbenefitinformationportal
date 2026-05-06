<script setup>
import { ref, onMounted } from 'vue'
import AppIcon from '@/components/common/AppIcon.vue'

const stats = ref([
  { label: 'Event', count: 0, icon: 'cilUser', colorClass: 'bg-blue', categoryId: 1 },
  { label: 'Bantuan', count: 0, icon: 'cilMoney', colorClass: 'bg-red', categoryId: 2 },
  { label: 'Tunjangan', count: 0, icon: 'cilWallet', colorClass: 'bg-green', categoryId: 3 },
  { label: 'Lainnya', count: 0, icon: 'cilHappy', colorClass: 'bg-purple', categoryId: 4 }
])

const fetchStats = async () => {
  try {
    const response = await fetch('http://localhost:5117/api/benefits/summary')
    if (!response.ok) throw new Error('Gagal mengambil data')

    const apiData = await response.json()

    stats.value = stats.value.map(stat => {
      const found = apiData.find(d => d.categoryId === stat.categoryId)
      return { ...stat, count: found ? found.count : 0 }
    })
  } catch (error) {
    console.error('Error dashboard:', error)
  }
}

onMounted(() => {
  fetchStats()
})
</script>

<template>
  <div class="dashboard-page">
    <!-- HEADER -->
    <div class="page-header mb-5">
      <div class="d-flex justify-content-between align-items-start flex-wrap gap-3">
        <div>
          <h2 class="mb-1 fw-bold text-dark-2">Dashboard</h2>
          <p class="page-subtitle mb-0">Ringkasan benefit per kategori</p>
        </div>
      </div>
    </div>

    <!-- STATS CARDS GRID -->
    <div class="row g-4 mb-5">
      <div 
        v-for="stat in stats" 
        :key="stat.categoryId"
        class="col-lg-3 col-md-6 col-sm-12"
      >
        <div class="stat-card h-100">
          <div class="card-body d-flex flex-column p-4">
            <!-- ✅ ICON BOX DENGAN COLOR -->
            <div class="icon-box {{ stat.colorClass }} mb-3">
              <AppIcon :name="stat.icon" class="text-white" />
            </div>

            <!-- CONTENT -->
            <div class="flex-grow-1">
              <h3 class="stat-number mb-1">{{ stat.count.toLocaleString() }}</h3>
              <p class="stat-label mb-0">{{ stat.label }}</p>
            </div>

            <!-- TREND -->
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.icon-box .text-white {
  filter: hue-rotate(200deg) brightness(1.2) saturate(1.5);
  color: #3b82f6 !important;
}

/* Atau lebih spesifik */
.stat-card .icon-box svg {
  color: #3b82f6 !important;
  filter: brightness(1.1);
}
</style>