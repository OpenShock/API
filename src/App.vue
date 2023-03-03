<template>
  <div id="app" class="app-height-100">
    <router-view/>
  </div>
</template>

<script>
export default {
  beforeMount() {
    this.$store.commit('setDarkMode', utils.isDarkMode());
  },
  watch: {
    '$store.state.settings.dark'(val, old) {
      $('body').attr('data-color-scheme', val ? 'dark' : 'white');
    }
  }
}
</script>