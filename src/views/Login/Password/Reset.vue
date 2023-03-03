<template>
  <form class="login100-form">
    <span class="login100-form-title">Forgot Password</span>

    <div class="signup-form" :class="{'fadeout': fadeout}" v-if="!requestStarted">

      <div class="wrap-input100 validate-input" v-bind:class="{'alert-validate': emailChecks.length > 0 && first.email}"
           :data-validate="emailChecks[0]">
        <span class="label-input100 ">Email</span>
        <input class="input100" @keydown="first.email = true" v-model="email" type="email" name="email"
               placeholder="Type your email">
        <span class="focus-input100 email"></span>
      </div>

      <div class="forgot-pw">
      </div>

      <div class="container-login100-form-btn">
        <div class="wrap-login100-form-btn">
          <div class="login100-form-bgbtn"></div>
          <button v-on:click.prevent="reset" class="login100-form-btn">Send Reset Email</button>
        </div>
      </div>

      <div class="sep-or">
        <span class="txt1">Back</span>
        <router-link :to="'/account/login'" class="txt2">Login</router-link>
      </div>
    </div>
    <loading-view v-else :loading="!requestDone" :error="!successful"
                  :successText="'<p><b>If the mentioned email is linked to an account, a password reset email has been sent.</b></p><p>Please click the provided button/link in the mail inorder to reset your password.</p>'"
                  :errorText="'<p><b>Something went terribly wrong.</b></p><p>Please check your internet connection or contact our support.</p>'"/>
  </form>
</template>

<script>
import axios from 'axios';
import SanityChecks from '../SanityChecks';
import LoadingView from '../LoadingView';

export default {
  components: {
    LoadingView
  },
  data() {
    return {
      email: "",
      successful: false,
      fadeout: false,
      requestStarted: false,
      requestDone: false,
      first: {
        email: false
      }
    }
  },
  methods: {
    async reset() {
      this.first.email = true;
      if (this.emailChecks.length > 0) return;

      this.fadeout = true;
      setTimeout(() => {
        this.requestStarted = true;
      }, 500)
      try {
        const res = await axios({
          method: 'POST',
          url: config.apiUrl + 'user/password/reset',
          data: {
            email: this.email
          }
        });
        this.successful = true;
      } catch (err) {
        toastr.error(err);
      }
      this.requestDone = true;
    }
  },
  computed: {
    emailChecks() {
      return SanityChecks.checkEmail(this.email);
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
