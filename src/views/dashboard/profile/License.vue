<template>
  <div class="row g-5 justify-content-center">
    <div class="col base-widget">
      <div>
        <h4>Redeem a Licence Key</h4>
        <b-input-group prepend-html="<i class='fas fa-key'></i>" class="mb-2 mr-sm-2 mb-sm-0">
          <b-form-input v-model="key" placeholder="License Key"></b-form-input>
        </b-input-group>
        <loading-button text="Redeem" :loading="loading" icon="fa-key" @click="clicked"/>
      </div>
    </div>
  </div>
</template>

<script>
import LoadingButton from "../../utils/LoadingButton";

export default {
  components: {LoadingButton},
  data() {
    return {
      key: "",
      loading: false,
      successful: false
    }
  },
  methods: {
    async clicked() {
      if (this.key === '') {
        toastr.error('License Key must contain a key');
        return;
      }
      this.loading = true;
      const res = await apiCall.makeCall('POST', 'user/redeemables/redeem', {
        key: this.key
      });
      if (res !== undefined) {
        this.successful = true;
        this.key = '';
        this.$swal('Success', 'Successfully redeemed license.', 'success');
      } else {
        this.$swal('Error', 'Error while redeeming license, please check if you got the licence key correct.', 'error');
      }
      this.loading = false;
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