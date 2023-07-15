<template>
  <b-button variant="nano">
    <span>
      <i :class="icon"></i>
      <span v-if="state"><i  class="fa-solid fa-toggle-on"></i></span>
      <span v-else><i class="fa-solid fa-toggle-off"></i></span>
    </span>
  </b-button>
</template>

<script>
export default {
  props: {
    'icon': String,
    'state': Boolean
  }
}
</script>

<style scoped lang="scss"></style>