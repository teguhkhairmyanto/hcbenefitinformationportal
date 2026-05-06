<template>
  <div class="admin-container">
    
    <!-- SIDEBAR -->
    <aside class="admin-sidebar">
      <div class="sidebar-header">
        <div class="logo">
          HC Portal <span class="admin-badge">Admin</span>
        </div>
      </div>

      <nav class="menu sidebar-menu">
        <router-link :to="{ name: 'AdminDashboard' }" class="menu-item">
          <AppIcon name="cilSpeedometer" class="icon" />
          Dashboard
        </router-link>

        <router-link :to="{ name: 'AdminBenefit' }" class="menu-item">
          <AppIcon name="cilList" class="icon" />
          Kelola Benefit
        </router-link>
        
        <router-link :to="{ name: 'AdminChatLog' }" class="menu-item">
        <AppIcon name="cilChatBubble" class="icon" />
        Follow Up Chat
        </router-link>
        
      </nav>
    </aside>

    <!-- MAIN -->
    <div class="admin-main-wrapper">

      <!-- TOPBAR -->
      <header class="header admin-topbar">
        <div class="page-info d-flex align-items-center">
          <div class="logo d-lg-none me-3">HC Portal</div>
        </div>

        <div class="user-profile d-flex align-items-center">
          <span class="me-3 small fw-medium d-none d-md-block">Administrator</span>
          <div class="avatar-circle">AD</div>
        </div>
      </header>

      <!-- CONTENT -->
      <main class="admin-content">
        <!-- ✅ CONTENT BODY - DENGAN PADDING TOP FIX -->
        <div class="admin-page-body">
          <router-view />
        </div>

      </main>

      <!-- FOOTER -->
      <footer class="footer admin-footer">
        <div class="footer-bottom">
          <div class="d-flex justify-content-between align-items-center w-100 px-4">
            <span>© 2026 HC Portal Admin System</span>
            <div class="footer-links d-none d-md-flex gap-3"></div>
          </div>
        </div>
      </footer>

    </div>
  </div>
</template>