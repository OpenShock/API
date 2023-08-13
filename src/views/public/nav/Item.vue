<template>
  <li class="nav-item" :class="{active: item.active}" ref="li">
    <a class="nav-link" @click="this.$parent.clicked(index)" v-html="item.html"></a>
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
