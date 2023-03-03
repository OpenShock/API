<template>
  <div class="row g-5 justify-content-center">
    <div class="col-6 base-widget">
      <b-container fluid>
        <h4>General Account Information</h4>
        <b-row class="my-1">
          <b-col sm="2">
            <label for="id">User ID:</label>
          </b-col>
          <b-col sm="10">
            <b-form-input id="id" plaintext v-model="user.id"></b-form-input>
          </b-col>
        </b-row>

        <b-row class="my-1">
          <b-col sm="2">
            <label for="username">Username:</label>
          </b-col>
          <b-col sm="10">
            <b-form-input id="username" plaintext v-model="user.username"></b-form-input>
          </b-col>
        </b-row>

        <b-row class="my-1">
          <b-col sm="2">
            <label for="email">Email:</label>
          </b-col>
          <b-col sm="10">
            <b-form-input id="email" plaintext v-model="user.email"></b-form-input>
          </b-col>
        </b-row>

        <b-row class="my-1">
          <b-col sm="2">
            <label for="permission">Permission:</label>
          </b-col>
          <b-col sm="10">
            <b-form-input id="permission" plaintext v-model="user.permission"></b-form-input>
          </b-col>
        </b-row>

        <b-row class="my-1">
          <b-col sm="2">
            <label>Verified:</label>
          </b-col>
          <b-col sm="10">
            <b-form-checkbox plaintext readonly v-model="user.isVerified"></b-form-checkbox>
          </b-col>
        </b-row>

        <b-row class="my-1">
          <b-col sm="2">
            <label>Premium:</label>
          </b-col>
          <b-col sm="10">
            <b-form-checkbox plaintext readonly v-model="user.isPremium"></b-form-checkbox>
          </b-col>
        </b-row>
      </b-container>
    </div>
  </div>
</template>

<script>
export default {
  computed: {
    user() {
      return this.$store.state.user;
    }
  }
}
</script>

<style scoped lang="scss">
.my-1 {
  .col-sm-2 {
    align-self: center;
  }
}
</style>
