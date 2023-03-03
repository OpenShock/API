<template>
  <div class="row users g-5 justify-content-center">
    <search/>
    <user/>
  </div>
</template>

<script>
import Search from "./Search";
import User from "./user/User";

export default {
  components: {User, Search}
}
</script>

<style scoped>
</style>
