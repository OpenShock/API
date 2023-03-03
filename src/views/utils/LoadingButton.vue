<template>
  <b-button variant="nano">
    <span v-show="loading"><i class="fas fa-cog fa-spin"></i> {{ text }}</span>
    <span v-show="!loading"><i class="fas" :class="icon"></i> {{ text }}</span>
  </b-button>
</template>

<script>
export default {
  props: ['loading', 'icon', 'text']
}
</script>