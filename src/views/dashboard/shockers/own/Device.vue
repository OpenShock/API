<template>
    <b-container>
        <b-row><p>{{ device.name }}</p></b-row>
        <b-row> <own-shocker v-for="item in device.shockers" :shocker="item" :key="item.id"></own-shocker> </b-row>
    </b-container>
</template>

<script>
import OwnShocker from './OwnShocker.vue'
    export default {
  components: { OwnShocker },
        props: ["device"]
    }
</script>