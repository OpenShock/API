<template>
  <div class="notice">
    <div v-if="loading">
      <loading/>
      <div v-html="loadingText"/>
    </div>
    <div v-else-if="!error">
      <checkmark/>
      <div v-html="successText"/>
    </div>
    <div v-else>
      <checkmark :error="true"/>
      <div v-html="errorText"/>
    </div>
  </div>
</template>

<script>
import Checkmark from '@/views/utils/Checkmark';
import Loading from '@/views/utils/Loading';

export default {
  components: {
    Checkmark, Loading
  },
  props: ['error', 'loading', 'successText', 'errorText', 'loadingText']
}
</script>

<style scoped lang="scss">
:deep(.checkmark) {
  margin: 0 auto !important;
}

.notice {
  text-align: center;

  :deep(p) {
    font-size: 20pt;
    margin-top: 70px;
  }

}
</style>
