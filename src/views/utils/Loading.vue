<template>
  <svg class="checkmark" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 52 52">
    <circle class="checkmark__circle" cx="26" cy="26" r="25" fill="none"/>
  </svg>
</template>

<script>
export default {
  name: "Loading"
}
</script>

<style scoped lang="scss">

.checkmark__circle {
  stroke-dasharray: 166;
  stroke-width: 2;
  stroke-miterlimit: 10;
  stroke: var(--color);
  fill: none;
  animation: stroke 0.6s infinite;
  transform-origin: center;
  animation-timing-function: linear;
  stroke-dashoffset: 130;
}

.checkmark {
  --color: #7ac142;

  width: 100px;
  height: 100px;
  border-radius: 50%;
  display: block;
  stroke-width: 2;
  stroke: #fff;
  stroke-miterlimit: 10;
  margin: 10% auto;
  box-shadow: inset 0 0 0 var(--color);
}

@keyframes stroke {
  100% {
    transform: rotate(360deg);
  }
}
</style>
