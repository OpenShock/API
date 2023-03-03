<template>

</template>

<script>

export default {
  data() {
    return {
      requestDone: false,
    }
  },
  async mounted() {
    this.$parent.$parent.accountType = "Patreon";
    const code = this.$router.currentRoute.value.query.code;
    if (code === undefined || code === "") {
      toastr.error("No oauth code was provided, please try again or contact our support.")
      await this.$router.push("/manager/profile/settings");
      return;
    }

    try {
      await apiCall.makeCall("POST", "user/connections/patreon", {code});
      this.$parent.$parent.success = true;
    } catch (err) {
      toastr.error(err, "Account Management")
    }
    this.$parent.$parent.loading = false;
    this.$parent.$parent.done();
  }

}
</script>