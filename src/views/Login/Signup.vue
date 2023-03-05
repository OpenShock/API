<template>
  <form class="login100-form">
    <span class="login100-form-title">Sign Up</span>

    <div class="signup-form" :class="{'fadeout': fadeout}" v-if="!requestStarted">
      <div class="wrap-input100 m-b-23 validate-input"
           v-bind:class="{'alert-validate': usernameChecks.length > 0 && first.username}"
           :data-validate="usernameChecks[0]">
        <span class="label-input100">Username</span>
        <input class="input100" @keydown="first.username = true" v-model="username" type="text" name="username"
               placeholder="Type your username">
        <span class="focus-input100 username"></span>
      </div>

      <div class="wrap-input100 m-b-23 validate-input" v-bind:class="{'alert-validate': emailChecks.length > 0 && first.email}"
           :data-validate="emailChecks[0]">
        <span class="label-input100 ">Email</span>
        <input class="input100" @keydown="first.email = true" v-model="email" type="email" name="email"
               placeholder="Type your email">
        <span class="focus-input100 email"></span>
      </div>

      <div class="wrap-input100 validate-input" v-bind:class="{'alert-validate': passwordChecks.length > 0 && first.password}"
           :data-validate="passwordChecks[0]">
        <span class="label-input100 ">Password</span>
        <input class="input100" @keydown="first.password = true" v-model="password" type="password" name="pass"
               placeholder="Type your password">
        <span class="focus-input100 password"></span>
      </div>

      <div class="forgot-pw">
      </div>

      <div class="container-login100-form-btn">
        <div class="wrap-login100-form-btn">
          <div class="login100-form-bgbtn"></div>
          <button v-on:click.prevent="signup" class="login100-form-btn">Create Account</button>
        </div>
      </div>

      <div class="sep-or">
        <span class="txt1">Or</span>
        <router-link :to="'/account/login'" class="txt2">Login</router-link>
      </div>
    </div>
    <div v-else>
      <loading-view :loading="!requestDone" :error="!successful"
                    :successText="'<p><b>We have sent an account activation mail to your inbox.</b></p><p>Please click the provided button/link in the mail inorder to activate your account.</p>'"
                    :errorText="errorMessage"/>
      <button @click.prevent="resetAll">Go back</button>
    </div>
  </form>
</template>

<script>
import axios from 'axios';
import SanityChecks from './SanityChecks';
import LoadingView from './LoadingView';

export default {
  components: {
    LoadingView
  },
  data() {
    return {
      username: "",
      password: "",
      email: "",
      successful: false,
      fadeout: false,
      requestStarted: false,
      requestDone: false,
      errorMessage: "<p><b>Something went terribly wrong.</b></p><p>Please check your internet connection or contact our support.</p>",
      first: {
        username: false,
        password: false,
        email: false
      }
    }
  },
  methods: {
    async signup() {
      console.log("cock");
      this.first.username = true;
      this.first.password = true;
      this.first.email = true;
      if (this.usernameChecks.length > 0 || this.emailChecks.length > 0 || this.passwordChecks.length > 0) {
        toastr.error("Please make sure to fullfill all requirements, (!) in the corresponding field");
        return;
      }

      this.fadeout = true;
      setTimeout(() => {
        this.requestStarted = true;
      }, 500)
      try {
        await axios({
          method: 'POST',
          url: config.apiUrl + '1/account/signup',
          data: {
            username: this.username,
            email: this.email,
            password: this.password
          }
        });
        this.successful = true;
      } catch (err) {
        if(err.response !== undefined && err.response.data.message !== undefined && err.response.data.message !== "") {
          this.errorMessage = '<p><b>Something went wrong.</b></p><p>' + err.response.data.message + '</p>';
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
    }
  },
  computed: {
    usernameChecks() {
      return SanityChecks.checkUsername(this.username);
    },
    emailChecks() {
      return SanityChecks.checkEmail(this.email);
    },
    passwordChecks() {
      return SanityChecks.checkPassword(this.password);
    }
  }
}
</script>

<style scoped>
.signup-form.fadeout {
  position: relative;
  height: calc(100% - 100px);

  left: 0;
  animation: slide 0.5s forwards;
}

@keyframes slide {
  100% {
    left: -500px;
  }
}
</style>
