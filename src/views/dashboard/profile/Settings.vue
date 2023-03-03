<template>
  <div class="row g-5 justify-content-center">
    <div class="col base-widget">
      <div>
        <h4>General Account Settings</h4>
        <b-input-group prepend="@" class="mb-2 mr-sm-2 mb-sm-0">
          <b-form-input id="inline-form-input-username" placeholder="Username"></b-form-input>
        </b-input-group>
      </div>
    </div>

    <div class="col base-widget">
      <div>
        <h4>Connections</h4>
        <loading v-if="!connectionInfo.requestDone"/>
        <div v-else>
          <loading-button v-if="!connectionInfo.data.PatreonConnected" text="Link To Patreon" icon="fab fa-patreon"
                          @click="patreon"/>
          <span v-else class="row justify-content-between m-0">
                <loading-button class="col-5" :loading="loading.patreonUnlink" text="Unlink Patreon" icon="fab fa-patreon"
                                @click="unlinkPatreon"/>
                <loading-button class="col-6" :loading="loading.patreonUpdate" text="Update Patreon Status"
                                icon="fab fa-patreon" @click="updatePatreon"/>
          </span>
        </div>
      </div>
    </div>

    <div class="col base-widget">
      <div>
        <h4>Logout</h4>
        <loading-button text="Logout" icon="fa-sign-out-alt" @click="logout"/>
      </div>
    </div>
  </div>
</template>

<script>
import LoadingButton from "../../utils/LoadingButton";
import apiCall from "../../../js/ApiCall";
import Loading from "../../utils/Loading";

export default {
  components: {Loading, LoadingButton},
  data() {
    return {
      connectionInfo: {
        requestDone: false,
        data: undefined
      },
      loading: {
        patreonUnlink: false,
        patreonUpdate: false
      }
    }
  },
  beforeMount() {
    this.getConnectionInfo();
  },
  methods: {
    logout() {
      this.$router.push('/account/login');
      utils.setLogin("");
    },
    patreon() {
      window.location.href = `https://www.patreon.com/oauth2/authorize?response_type=code&client_id=${config.patreon.clientId}&redirect_uri=${config.patreon.redirectUri}&scope=identity`;
    },
    async updatePatreon() {
      this.loading.patreonUpdate = true;
      await apiCall.makeCall("PATCH", "user/connections/patreon");
      await this.$store.dispatch('getSelf');
      this.loading.patreonUpdate = false;
    },
    async unlinkPatreon() {
      this.loading.patreonUnlink = true;
      await apiCall.makeCall("DELETE", "user/connections/patreon");
      await this.getConnectionInfo();
      this.loading.patreonUnlink = false;
    },
    async getConnectionInfo() {
      this.connectionInfo.requestDone = false;
      const res = await apiCall.makeCall("GET", "user/connections");
      this.connectionInfo.data = res.data.Data;
      this.connectionInfo.requestDone = true;
    }
  }
}
</script>

<style scoped lang="scss">
.base-widget {
  min-width: 150px;
  max-width: 500px;
}
</style>