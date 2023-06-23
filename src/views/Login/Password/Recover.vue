<template>
  <form class="login100-form">
    <span class="login100-form-title">Recovery</span>

    <loading v-if="!ticket.getRequestDone"/>
    <div class="signup-form" :class="{'fadeout': fadeout}" v-else-if="ticket.found && !ticket.postRequestStarted">

      <div class="wrap-input100" v-bind:class="{'alert-validate': passwordChecks.length > 0 && first.password}"
           :data-validate="passwordChecks[0]">
        <span class="label-input100 ">Password</span>
        <input class="input100" @keydown="first.password = true" v-model="password" type="password" name="pass"
               placeholder="Type your password">
        <span class="focus-input100 password"></span>
      </div>

      <div class="wrap-input100" v-bind:class="{'alert-validate': password !== passwordConfirm && first.password}"
           :data-validate="'Passwords do not match'">
        <span class="label-input100 ">Password</span>
        <input class="input100" @keydown="first.password = true" v-model="passwordConfirm" type="password" name="pass"
               placeholder="Confirm your password">
        <span class="focus-input100 password"></span>
      </div>

      <div class="forgot-pw">
      </div>

      <div class="container-login100-form-btn">
        <div class="wrap-login100-form-btn">
          <div class="login100-form-bgbtn"></div>
          <button v-on:click.prevent="recover" class="login100-form-btn">Change Password</button>
        </div>
      </div>


    </div>
    <loading-view v-else :loading="!ticket.postRequestDone && ticket.found"
                  :error="!ticket.postSuccessful || !ticket.found"
                  :successText="'<p><b>Successfully changed your password.</b></p><p>You can login now, using your newly set password.</p>'"
                  :errorText="'<p><b>Recovery ticket could not be found.</b></p><p>Have you already reset your password?</p>'"/>
    <div class="sep-or">
      <span class="txt1">Back</span>
      <router-link :to="'/account/login'" class="txt2">Login</router-link>
    </div>
  </form>
</template>

<script>
import axios from 'axios';
import LoadingView from '../LoadingView';
import SanityChecks from '../SanityChecks';
import Loading from '@/views/utils/Loading';

export default {
  components: {
    LoadingView, Loading
  },
  props: ["uuid", "secret"],
  data() {
    return {
      password: "",
      passwordConfirm: "",
      ticket: {
        getRequestDone: false,
        found: false,
        postRequestDone: false,
        postRequestStarted: false,
        postSuccessful: false
      },
      fadeout: false,
      first: {
        password: false
      }
    }
  },
  mounted() {
    this.checkForRecovery();
  },
  methods: {
    async checkForRecovery() {
      try {
        await axios({
          method: 'HEAD',
          url: this.recoveryUrl
        });
        this.ticket.found = true;
      } catch (err) {
        toastr.error(err);
      }
      this.ticket.getRequestDone = true;
    },
    async recover() {
      this.first.password = true;
      if (this.passwordChecks.length > 0 || this.password !== this.passwordConfirm) return;
      this.fadeout = true;
      setTimeout(() => {
        this.ticket.postRequestStarted = true;
      }, 500)
      try {
        await axios({
          method: 'POST',
          url: this.recoveryUrl,
          data: {
            Password: this.password
          }
        });
        this.ticket.postSuccessful = true;
        this.ticket.postRequestDone = true;
      } catch (err) {
        toastr.error(err);
      }

    }
  },
  computed: {
    passwordChecks() {
      return SanityChecks.checkPassword(this.password);
    },
    recoveryUrl() {
      return config.apiUrl + '1/account/recover/' + this.uuid + "/" + this.secret;
    }
  }
}
</script>

<style scoped>
.signup-form {
  opacity: 1;
  animation-name: fadeInOpacity;
  animation-iteration-count: 1;
  animation-timing-function: ease-in;
  animation-duration: 0.3s;
}

@keyframes fadeInOpacity {
  0% {
    opacity: 0;
  }
  100% {
    opacity: 1;
  }
}

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
