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
        routerLink: '/shockers/own',
        html: '<i class="fa-solid fa-house"></i>Own'
      },
      {
        routerLink: '/shockers/shared',
        html: '<i class="fa-solid fa-share-nodes"></i>Shared'
      },
    ]);
  }
}
</script>
