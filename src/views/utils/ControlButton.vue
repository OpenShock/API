<template>
  <b-button variant="nano" class="control-button" :class="loading ? 'loading' : ''">
    <span v-show="loading"><i class="fas" :class="loadingIcon"></i> {{ text }}</span>
    <span v-show="!loading"><i :class="icon"></i> {{ text }}</span>
  </b-button>
</template>

<script>
export default {
  emits: ['onStart', 'onStop'],
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

<style scoped lang="scss">
.loading {
  &:hover {
    background-color: var(--swal-error-line);
    &:focus {
      box-shadow: 0 0 0 0.25rem var(--swal-error-line);
    }
  }

}
</style>