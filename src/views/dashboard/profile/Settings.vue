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
        requestDone: true,
        data: undefined
      },
      loading: {
        patreonUnlink: false,
        patreonUpdate: false
      }
    }
  },
  beforeMount() {
    //this.getConnectionInfo();
  },
  methods: {
    logout() {
      this.$router.push('/account/login');
      utils.setLogin("");
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