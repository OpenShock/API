<template>
  <div class="theme-toggle row justify-content-center align-items-center"
       @click="$store.dispatch('setDarkMode', !dark)">
    <span v-show="dark"><i class="fas fa-moon col"></i></span>
    <span v-show="!dark"><i class="fas fa-sun col"></i></span>
  </div>
</template>

<script>
export default {
  computed: {
    dark() {
      return this.$store.state.settings.dark;
    }
  }
}
</script>

<style scoped lang="scss">
.theme-toggle {
  border-radius: 70px;
  width: 32px;
  height: 32px;
  cursor: pointer;

  span {
    width: auto;
    padding: 0;

    svg {
      width: 16px;
      height: 16px;
      padding: 0;
      color: var(--main-text-color);

      transition: color ease-in-out .2s;
    }
  }

  transition: background-color ease-in-out .2s;

  &:hover {
    background-color: var(--hover-darken-color);

    span svg {

    }
  }
}
</style>