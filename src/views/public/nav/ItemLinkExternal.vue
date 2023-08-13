<template>
  <li class="nav-item" ref="li">
    <a class="nav-link" @click="clicked"><slot></slot></a>
  </li>
</template>

<script>
export default {
  props: ["link"],
  mounted() {

  },
  methods: {
    clicked() {
      window.location = this.link;
    }
  }
}
</script>
