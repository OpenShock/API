<template>
  <div>
    <div class="row input-uid">
      <form class="col">
        <label for="userId">User ID</label>
        <input type="text" class="form-control" id="userId" v-model="userId" placeholder="Enter a user id">
      </form>
      <button class="col btn btn-nano scuff" @click="get" :disabled="loading">
        <span v-show="loading"><i class="fas fa-cog fa-spin"></i></span>
        <span v-show="!loading"><i class="fas fa-search"></i></span>
      </button>
    </div>
    <label for="username">Username</label>
    <input type="text" class="form-control" id="username" v-model="result.Username" readonly>

    <label for="email">Email</label>
    <input type="text" class="form-control" id="email" v-model="result.Email" readonly>

    <label for="permission">Permission</label>
    <input type="text" class="form-control" id="permission" v-model="result.Permission" readonly>

    <label for="created">Creation Date</label>
    <input type="text" class="form-control" id="created" v-model="result.CreationDate" readonly>

    <label for="createdIp">Created IP</label>
    <input type="text" class="form-control" id="createdIp" v-model="result.CreatedIp" readonly>

    <div class="form-check form-switch">
      <input class="form-check-input" type="checkbox" id="isEmailVerified" v-model="result.IsEmailVerified" readonly>
      <label class="form-check-label" for="isEmailVerified">Is Email Verified</label>
    </div>

    <div class="form-check form-switch">
      <input class="form-check-input" type="checkbox" id="isVerified" v-model="result.IsVerified" readonly>
      <label class="form-check-label" for="isVerified">Is Verified</label>
    </div>

    <div class="form-check form-switch">
      <input class="form-check-input" type="checkbox" id="isPremium" v-model="result.IsPremium" readonly>
      <label class="form-check-label" for="isPremium">Is Premium</label>
    </div>
  </div>
</template>

<script>
export default {
  props: ['requestedUser'],
  data() {
    return {
      userId: "",
      result: {
        "Username": "none",
        "Email": "none",
        "Permission": 0,
        "CreationDate": "1970-01-01T00:00:00",
        "CreatedIp": "0.0.0.0",
        "IsEmailVerified": false,
        "IsVerified": false,
        "IsPremium": false
      },
      loading: false
    }
  },
  mounted() {
    if (this.requestedUser !== '') {
      this.userId = this.requestedUser;
      this.get();
    }
  },
  methods: {
    async get() {
      if (this.userId === "") {
        toastr.error("User ID must be non empty");
        return;
      }
      this.loading = true;
      const res = await apiCall.makeCall('GET', 'admin/users/' + this.userId, undefined);
      this.loading = false;
      this.result = res.data.Data;
    }
  },
  watch: {
    'requestedUser'(newA, old) {
      this.userId = newA;
      this.get();
    }
  }
}
</script>

<style scoped lang="scss">

</style>
