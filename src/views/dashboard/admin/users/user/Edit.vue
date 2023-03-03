<template>
  <div>
    <div>
      <div class="row input-uid">
        <form class="col">
          <label for="userId">User ID</label>
          <input type="text" class="form-control" id="userId" v-model="userId" placeholder="Enter a user id to edit">
        </form>
        <button class="col btn btn-nano scuff" @click="get" :disabled="loading.search">
          <span v-show="loading.search"><i class="fas fa-cog fa-spin"></i></span>
          <span v-show="!loading.search"><i class="fas fa-search"></i></span>
        </button>
      </div>
      <label for="username">Username</label>
      <input type="text" class="form-control" id="username" v-model="form.Username" placeholder="Users username">

      <label for="email">Email</label>
      <input type="text" class="form-control" id="email" v-model="form.Email" placeholder="Users email">

      <label for="permission">Permission</label>
      <input type="text" class="form-control" id="permission" v-model="form.Permission" placeholder="Permission level">

      <div class="row mb-2 mt-2">
        <div class="col">
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
        </div>
        <button class="col align-self-end btn btn-nano submit-btn" @click="update" :disabled="loading.update">
          <span v-show="loading.update"><i class="fas fa-cog fa-spin"></i> Send</span>
          <span v-show="!loading.update"><i class="fas fa-user-edit"></i> Send</span>
        </button>
      </div>
    </div>

    <div class="new-password row">


      <form class="col">
        <label for="password">New Password</label>
        <input type="text" class="form-control" id="password" v-model="newPassword" placeholder="Enter a new password">
      </form>
      <button class="col btn btn-nano scuff" @click="updatePassword" :disabled="loading.password">
        <span v-show="loading.password"><i class="fas fa-cog fa-spin"></i></span>
        <span v-show="!loading.password"><i class="fas fa-user-edit"></i></span>
      </button>
    </div>
  </div>
</template>

<script>
export default {
  props: ['requestedUser'],
  data() {
    return {
      userId: "",
      form: {
        "Username": "",
        "Email": "",
        "Permission": 0,
        "IsEmailVerified": false,
        "IsVerified": false,
        "IsPremium": false
      },
      newPassword: "",
      loading: {
        search: false,
        update: false,
        password: false
      }
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
      this.loading.search = true;
      const res = await apiCall.makeCall('GET', 'admin/users/' + this.userId, undefined);
      this.loading.search = false;
      this.form = res.data.Data;
    },
    async update() {
      if (this.userId === "") {
        toastr.error("User ID must be non empty");
        return;
      }
      this.loading.update = true;
      const res = await apiCall.makeCall('POST', 'admin/users/' + this.userId, this.form);
      this.loading.update = false;
      toastr.success('Successfully updated user', 'User Management');
      this.form = {
        "Username": "",
        "Email": "",
        "Permission": 0,
        "IsEmailVerified": false,
        "IsVerified": false,
        "IsPremium": false
      };
      this.userId = "";

    },
    async updatePassword() {
      if (this.userId === "") {
        toastr.error("User ID must be non empty");
        return;
      }
      if (this.newPassword === "") {
        toastr.error("New Password must be non empty");
        return;
      }

      this.loading.password = true;
      const res = await apiCall.makeCall('POST', 'admin/users/' + this.userId + '/password', {
        password: this.newPassword
      });
      this.loading.password = false;
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
.submit-btn {
  flex: 0 0 auto;
  width: 150px;
  height: 40px;
}

.new-password {
  margin-top: 25px;
}
</style>
