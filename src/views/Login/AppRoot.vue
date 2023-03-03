<template>
  <div class="container-login100">
    <div class="wrap-login100">
      <router-view v-slot="{ Component }">
        <transition name="component-fade" mode="out-in">
          <component :is="Component"></component>
        </transition>
      </router-view>
    </div>
  </div>

  <div id="dropDownSelect1"></div>
</template>

<script>
import ThemeToggle from "../utils/ThemeToggle";
export default {
  components: {ThemeToggle},
  mounted() {
    $('body').addClass('login');
  },
  unmounted() {
    $('body').removeClass('login');
  },
}
</script>


<style>
@import "./LoginStyle.scss";

body.login {
  background: url("../../assets/images/background6.jpg") center center / cover no-repeat fixed !important;
}
</style>

<style scoped>
.theme-toggle-fix {
  margin-top: -32px;
  float: right;
}
</style>
