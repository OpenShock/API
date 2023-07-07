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
      
    ]);
  }
}
</script>
