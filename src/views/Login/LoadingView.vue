<template>
  <div class="notice">
    <loading v-if="loading"/>
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
  props: ['error', 'loading', 'successText', 'errorText']
}
</script>

<style scoped lang="scss">

.notice {
  text-align: center;
  position: relative;

  left: 500px;
  animation: slide 0.5s forwards;

  :deep(p) {
    font-size: 20pt;
    margin-bottom: 40px;
  }

  .checkmark {
    margin-top: 10px;
    margin-bottom: 50px;
  }

  @keyframes slide {
    100% { left: 0; }
  }
}

</style>
