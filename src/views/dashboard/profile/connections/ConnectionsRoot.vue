<template>
  <div>
    <loading-view class="loading-view" :loading="loading" :error="!success"
                  :success-text="successText" :loading-text="loadingText" :error-text="errorText"></loading-view>
    <router-view v-slot="{ Component }">
      <component :is="Component"></component>
    </router-view>
  </div>
</template>

<script>
import LoadingView from "../../../utils/LoadingView";

export default {
  components: {LoadingView},
  data() {
    return {
      loading: true,
      success: false,
      accountType: undefined
    }
  },
  methods: {
    done() {
      setTimeout(async () => {
        await this.$router.push("/manager/profile/settings");
      }, 4000);
    }
  },
  computed: {
    successText() {
      return `<p>Successfully linked ${this.accountType} account</p>`
    },
    errorText() {
      return `<p>Error linking ${this.accountType} account</p>`
    },
    loadingText() {
      return `<p>Please wait while your ${this.accountType} account is being linked</p>`
    }
  }
}
</script>

<style scoped lang="scss">
.loading-view {
  margin: 10% auto;
}
</style>