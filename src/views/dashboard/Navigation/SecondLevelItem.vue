<template>
  <li class="nav-item">
    <a class="nav-link" v-on:click="clicked" v-html="item.html"/>
  </li>
</template>

<script>
export default {
  props: ["item"],
  mounted() {
    this.item.that = this;
  },
  methods: {
    clicked() {
      this.$router.push('/dashboard' + this.item.routerLink)
    }
  }
}
</script>
