<template>
  <div class="login100-form">
    <span class="login100-form-title">Account Activation</span>
    <div v-if="loading">
      <loading/>
    </div>
    <div v-else>
      <div v-if="successful">
        <checkmark/>
        <div class="inner">
          <p><b>Your account has been activated successfully!</b></p>
          <p>You can now login with your credentials.</p>
        </div>
      </div>
      <div v-else>
        <checkmark :error="true"/>
        <p><b>Could not find activation ticket.</b></p>
        <p>Did you already activate your account?<br>Try to login below.</p>
      </div>
      <div class="sep-or">
        <router-link :to="'/account/login'" class="txt2">Login</router-link>
      </div>
    </div>
  </div>
</template>

<script>
import axios from 'axios';
import Checkmark from '@/views/utils/Checkmark';
import Loading from '@/views/utils/Loading';

export default {
  name: "Activate",
  components: {
    Checkmark,
    Loading
  },
  props: ['uuid', 'secret'],
  mounted() {
    this.sendActivation();
  },
  methods: {
    async sendActivation() {
      const res = await axios({
        method: 'POST',
        url: config.apiUrl + 'user/activate',
        data: {
          uuid: this.uuid,
          secret: this.secret
        }
      }).catch(err => {
        this.loading = false;
        this.successful = false;

      });
      if (this.loading) {
        this.loading = false;
        this.successful = true;
      }
    }
  },
  data() {
    return {
      loading: true,
      successful: false
    }
  }
}
</script>

<style scoped lang="scss">
.login100-form {
  text-align: center;

  .inner {
    position: relative;
    animation: slide 0.5s forwards;
    left: 500px;

    @keyframes slide {
      100% {
        left: 0;
      }
    }
  }

  p {
    font-size: 20pt;
    margin-bottom: 40px;
  }

  .checkmark {
    margin-top: 10px;
    margin-bottom: 50px;
  }


}
</style>
