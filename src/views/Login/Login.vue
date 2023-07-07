<template>
  <form class="login100-form">
    <span class="login100-form-title">Login</span>

    <div class="signup-form" :class="{'fadeout': fadeout}" v-if="!requestStarted">
      <div class="wrap-input100 m-b-23 validate-input"
           v-bind:class="{'alert-validate': username === '' && first.username}"
           :data-validate="'Email is Required'">
        <span class="label-input100">Email</span>
        <input class="input100" @keydown="first.username = true" v-model="username" type="email" name="username"
               placeholder="Type your email">
        <span class="focus-input100 username" data-symbol="person_outline"></span>
      </div>

      <div class="wrap-input100 validate-input" v-bind:class="{'alert-validate': password === '' && first.password}"
           :data-validate="'Password is Required'">
        <span class="label-input100 ">Password</span>
        <input class="input100" @keydown="first.password = true" v-model="password" type="password" name="pass"
               placeholder="Type your password">
        <span class="focus-input100 password" data-symbol="&#xf190;"></span>
      </div>

      <div class="forgot-pw">
        <router-link :to="'/account/password/reset'">Forgot password?</router-link>
      </div>

      <div class="container-login100-form-btn">
        <div class="wrap-login100-form-btn">
          <div class="login100-form-bgbtn"></div>
          <button v-on:click.prevent="sendLogin" class="login100-form-btn">Login</button>
        </div>
      </div>

      <div class="sep-or">
        <span class="txt1">Or</span>
        <router-link :to="'/account/signup'" class="txt2">Register</router-link>
      </div>
    </div>
    <div v-else>
      <loading-view :loading="!requestDone" :error="!successful"
                    :successText="'<p><b>Successfully logged in!</b></p><p>Redirecting to <br><b><u>ShockLink Dashboard</u></b></p>'"
                    :errorText="errorMessage"/>
      <div class="sep-or">
        <a class="txt2" @click="resetAll">Go Back</a>
      </div>
    </div>
  </form>
</template>

<script>
import axios from 'axios';
import LoadingView from './LoadingView';

export default {
  components: {
    LoadingView
  },
  data() {
    return {
      username: "",
      password: "",
      errorMessage: "<p><b>Something went terribly wrong.</b></p><p>Please check your internet connection or contact our support.</p>",
      fadeout: false,
      requestStarted: false,
      requestDone: false,
      successful: false,
      first: {
        username: false,
        password: false
      }
    }
  },
  methods: {
    async sendLogin() {
      if (this.username === "" || this.password === "") return;

      this.fadeout = true;
      setTimeout(() => {
        this.requestStarted = true;
      }, 300)
      try {
        const res = await axios({
          method: 'POST',
          url: config.apiUrl + '1/account/login',
          data: {
            username: this.username,
            email: this.username,
            password: this.password
          }
        });
        this.successful = true;
        utils.setLogin();
        setTimeout(() => {
          const returnUrl = this.$store.state.returnUrl;
          if(returnUrl !== undefined) {
            this.$store.dispatch('setReturnUrl', undefined);
            this.$router.push(returnUrl);
            return;
          }
          this.$router.push('/dashboard/');
        }, 2500)
      } catch (err) {
        if (err.response !== undefined) {
          switch (err.response.status) {
            case 401:
                this.errorMessage = '<p><b>Credentials did not match any account.</b></p>';
              break;
            case 403:
              this.errorMessage = '<p><b>Account not activated</b></p>';
              break;
            default:
              if(err.response.data.Message !== undefined && err.response.data.message !== "") {
                this.errorMessage = '<p><b>Something went wrong.</b></p><p>' + err.response.data.message + '</p>';
              } else {
                toastr.error(err);
              }
              break;
          }
        } else {
          toastr.error(err);
        }
      }
      this.requestDone = true;
    },
    resetAll() {
      this.successful = false;
      this.fadeout = false;
      this.requestStarted = false;
      this.requestDone = false;
      this.errorMessage = "<p><b>Something went terribly wrong.</b></p><p>Please check your internet connection or contact our support.</p>";
    }
  }
}
</script>

<style scoped>
.signup-form.fadeout {
  position: relative;
  height: calc(100% - 100px);

  left: 0;
  animation: slide 0.3s forwards;
}

@keyframes slide {
  100% {
    left: -500px;
  }
}
</style>
