<template>
  <div class="base-wrap">
    <router-view v-slot="{ Component }">
      <transition name="component-fade" mode="out-in">
        <component :is="Component"></component>
      </transition>
    </router-view>
  </div>
</template>

<script>
export default {
  mounted() {
    this.$store.dispatch('setNewNav', [
      {
        routerLink: '/profile/account',
        html: '<i class="fas fa-user"></i>Account'
      },
      {
        routerLink: '/profile/license',
        html: '<i class="fas fa-key"></i>License'
      },
      {
        routerLink: '/profile/settings',
        html: '<i class="fas fa-cog"></i>Settings'
      }
    ]);
  }
}
</script>
