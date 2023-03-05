<template>
  <b-button variant="nano">
    <span v-show="loading"><i class="fas" :class="loadingIcon"></i> {{ text }}</span>
    <span v-show="!loading"><i :class="icon"></i> {{ text }}</span>
  </b-button>
</template>

<script>
export default {
  props: {
    'loading': Boolean,
    'icon': String,
    'text': String,
    'loadingIcon': {
      type: String,
      default: 'fas fa-cog fa-spin'
    }
  }
}
</script>