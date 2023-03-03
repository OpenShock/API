<template>
<p>Redirecting...</p>
</template>

<script>
export default {
  beforeMount() {
    this.redirect();
  },
  methods: {
    redirect() {
      console.log("Redirecting...");
      if(utils.authExists()) {
        this.$router.push('/dashboard/');
      } else {
        this.$router.push('/account/');
      }
    }
  }
}
</script>

<style scoped>

</style>
