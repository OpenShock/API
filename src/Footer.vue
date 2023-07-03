<template>
  <p class="version">ShockLink UI 1.1.0
    <a target="_blank" :href="'https://github.com/Shock-Link/WebUI/commit/' + commitHash">{{ commitHash }}</a> | API <span
      v-html="apiVersion"></span> <a target="_blank"
      :href="'https://github.com/Shock-Link/API/commit/' + apiCommitHash">{{ apiCommitHash }}</a>
  </p>
  <p class="cred"><a href="https://github.com/Shock-Link">Made by ShockLink Team</a></p>
  <p class="status" :style="'color: #' + connectionColor + ';'" v-html="$store.state.userHubState"></p>
</template>
<script>
import axios from 'axios';

export default {
  data() {
    return {
      apiVersion: "Loading...",
      apiCommitHash: ""
    }
  },
  async beforeMount() {
    await this.getApiVersion();
  },
  methods: {
    async getApiVersion() {
      try {
        const res = await axios({
          method: 'GET',
          url: config.apiUrl + '1/'
        });
        this.apiVersion = res.data.data.version;
        this.apiCommitHash = res.data.data.commit.substring(0, 7);
      } catch (err) {
        console.log(err);
        this.apiVersion = "Error"
      }
    }
  },
  computed: {
    commitHash() {
      return COMMIT_HASH.substring(0, 7);
    },
    connectionColor() {
      switch(this.$store.state.userHubState) {
        case "Initializing":
          return "808080";
        case "Reconnecting":
        case "Connecting":
          return "ffbf00";
        case "Connected":
          return "00ff1a";
        case "Disconnected":
          return "ff0000";
      }
    }
  }
}
</script>

<style lang="scss">
footer {
  position: fixed;
  bottom: 0;
  height: 19px;
  width: 100%;
  border-top: 1px solid var(--secondary-background-color);
  background-color: var(--main-blackground-dark);
  padding: 0 5px;
  display: flex;
  flex-direction: row;
  font-size: 12px;
  color: var(--secondary-color);
  justify-content: space-between;

  p {
    margin-bottom: 0;
    width: 33.3%
  }

  a {
    text-decoration: none;
  }

  .version a {
    color: var(--fourth-background-color);
  }

  .cred {
    text-align: center;

    a {
      color: var(--secondary-color);
    }
  }

  .status {
    text-align: end;
  }
}</style>