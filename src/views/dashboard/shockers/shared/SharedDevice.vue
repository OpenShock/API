<template>
    <b-container class="device">
        <b-row>
            <p>{{ device.name }}</p>
        </b-row>
        <b-row>
            <b-col v-for="item in device.shockers" :key="item.id" class="shocker-col">
                <shared-shocker :shocker="item"></shared-shocker>
            </b-col>
        </b-row>
    </b-container>
</template>

<script>
import SharedShocker from './SharedShocker.vue';
export default {
  components: { SharedShocker },
    props: ['device'],
    beforeMount() {
        this.device.state = {
            online: false
        };

        this.device.shockers.forEach(shocker => {
            shocker.state = {
                intensity: 25,
                duration: 1000
            }
        });
    }
}
</script>
