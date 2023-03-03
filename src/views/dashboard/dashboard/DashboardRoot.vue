<template>
  <div class="base-wrap">

  </div>
</template>

<script>

export default {
  mounted() {
    this.$store.dispatch('setNewNav', []);
  }
}
</script>
