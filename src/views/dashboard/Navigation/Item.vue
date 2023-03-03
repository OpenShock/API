<template>
  <li class="nav-item" :class="{active: item.active, profile: item.profile}" ref="li">
    <a class="nav-link" v-on:click="this.$parent.clicked(index)" v-html="item.html"/>
  </li>
</template>

<script>
export default {
  props: ["item", "index"],
  mounted() {
    this.item.that = this;
  }
}
</script>
