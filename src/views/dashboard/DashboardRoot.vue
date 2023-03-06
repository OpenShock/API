<template>
  <router-view v-slot="{ Component }">
    <transition name="component-fade" mode="out-in">
      <div v-if="self.transitionFinished" :key="'0'" id="app-root"
           class="app-height-100 manager-root">
        <nav-root></nav-root>

        <transition name="component-fade" mode="out-in">
          <component :is="Component"></component>
        </transition>

      </div>
      <div v-else :key="'1'" class="app-height-100 manager-root-loading row align-items-center justify-content-center">
        <loading-view class="col-1" style="width: fit-content" :loading="self.loading" :error="!self.success"
                      :loading-text="'<p>Authenticating user.<br>Please wait...</p>'"
                      :success-text="'<p>Successfully authenticated.<br>Now loading <b><u>Dashboard</u></b></p>'"/>
      </div>
    </transition>
  </router-view>
</template>

<script>
require("@/js/SlWs.js");
import NavRoot from "./Navigation/NavRoot";
import LoadingView from "../utils/LoadingView";

export default {
  components: {NavRoot, LoadingView},
  mounted() {
    this.getSelf();
  },
  data() {
    return {
      self: {
        loading: true,
        transitionFinished: false,
        success: false
      }
    }
  },
  methods: {
    async getSelf() {
      this.self.loading = true;
     try {
       await this.$store.dispatch('getSelf');
       this.self.success = true;
     } catch (_) {}
     this.self.loading = false;

      setTimeout(() => {
        this.self.transitionFinished = true;
      }, 1500);
    }
  }
}
</script>

<style lang="scss">
@import "DashboardRoot";
</style>
