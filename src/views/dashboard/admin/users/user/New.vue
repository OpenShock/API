<template>
  <div>
    <label for="username">Username</label>
    <input type="text" class="form-control" id="username" v-model="form.Username" placeholder="Username">

    <label for="email">Email</label>
    <input type="text" class="form-control" id="email" v-model="form.Email" placeholder="Email">

    <label for="password">Password</label>
    <input type="text" class="form-control" id="password" v-model="form.Password" placeholder="Password">

    <label for="permission">Permission</label>
    <input type="text" class="form-control" id="permission" v-model="form.Permission" placeholder="Permission level">

    <div class="form-check form-switch">
      <input class="form-check-input" type="checkbox" id="isEmailVerified" v-model="form.IsEmailVerified">
      <label class="form-check-label" for="isEmailVerified">Is Email Verified</label>
    </div>

    <div class="form-check form-switch">
      <input class="form-check-input" type="checkbox" id="isVerified" v-model="form.IsVerified">
      <label class="form-check-label" for="isVerified">Is Verified</label>
    </div>

    <div class="form-check form-switch">
      <input class="form-check-input" type="checkbox" id="isPremium" v-model="form.IsPremium">
      <label class="form-check-label" for="isPremium">Is Premium</label>
    </div>

    <button class="col align-self-end btn btn-nano submit-btn" @click="send" :disabled="loading">
      <span v-show="loading"><i class="fas fa-cog fa-spin"></i> Create User</span>
      <span v-show="!loading"><i class="fas fa-user-plus"></i> Create User</span>
    </button>
  </div>
</template>

<script>
export default {
  data() {
    return {
      form: {
        "Username": "",
        "Email": "",
        "Password": "",
        "Permission": 0,
        "IsEmailVerified": false,
        "IsVerified": false,
        "IsPremium": false
      },
      loading: false
    }
  },
  methods: {
    async send() {
      if (this.form.Username === "" || this.form.Email === "" || this.form.Password === "" || this.form.Permission === undefined) {
        toastr.error("All fields must be filled");
        return;
      }
      this.loading = true;
      const res = await apiCall.makeCall('POST', 'admin/users/create', this.form);
      this.loading = false;
      toastr.success('Successfully created user', 'User Management');
      this.form = {
        "Username": "",
        "Email": "",
        "Password": "",
        "Permission": 0,
        "IsEmailVerified": false,
        "IsVerified": false,
        "IsPremium": false
      }
    }
  }
}
</script>

<style scoped lang="scss">

</style>
