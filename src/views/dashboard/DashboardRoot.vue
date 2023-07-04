<template>
  <router-view v-slot="{ Component }">
    <transition name="component-fade" mode="out-in">
      <div v-if="self.transitionFinished && userHubOk && self.success" :key="'0'" id="app-root"
           class="manager-root">
        <nav-root></nav-root>

        <transition name="component-fade" mode="out-in">
          <component :is="Component"></component>
        </transition>

      </div>
      <div v-else :key="'1'" class="manager-root-loading row align-items-center justify-content-center">
        <loading-view class="col-1" style="width: fit-content" :loading="isLoading" :error="!self.success || this.$store.state.userHubState == 'Disconnected'"
                      :loading-text="loadingText"
                      :success-text="successText"
                      :errorText="errorText"/>
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
  computed: {
    userHubOk() {
      return this.$store.state.userHubState == "Connected";
    },
    userHubInitiallyConnected() {
      const us = this.$store.state.userHubState;
      return us == "Reconnecting" || us == "Connected";
    },
    isLoading() {
      if(this.$store.state.userHubState == "Disconnected") return false;
      return this.self.loading || !this.userHubOk;
    },
    loadingText() {
      if(this.loading) return "<p>Authenticating user.<br>Please wait...</p>"
      if(this.userHubInitiallyConnected) return "<p>Trying to reconnect.<br>Please wait...</p>"
      return "<p>Trying to connect to user hub.<br>Please wait...</p>";
    },
    successText() {
      return "<p>Successfully connected.<br>Now loading <b><u>Dashboard</u></b></p>";
    },
    errorText() {
      if(!this.self.success) return "<p>Failed to connect to backend<br>Please try to reload the page</p>";
      if(!this.userHubInitiallyConnected) return "<p>Could not connect to hub.<br>Please try to reload the page</p>";
      if(!this.userHubOk) return "<p>Could not reconnect to hub.<br>Please try to reload the page</p>";

      return "<p>Failed to connect to backend.<br>Please try to reload the page</p>";
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
