<template>
    <p class="version">ShockLink UI 1.0.0 <a :href="'https://github.com/Shock-Link/WebUI/commit/' + commitHash">{{ commitHash }}</a></p>
    <p class="cred"><a href="https://github.com/Shock-Link">Made by ShockLink Team</a></p>
    <p class="commit"></p>
</template>
<script>
export default {
  computed: {
    commitHash() {
        return COMMIT_HASH.substring(0, 7);
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

  .commit {
    text-align: end;
  }
}
</style>