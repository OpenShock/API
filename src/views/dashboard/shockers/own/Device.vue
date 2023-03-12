<template>
    <b-container class="device">
        <b-row><b-col cols="auto"><p>{{ device.name }}</p></b-col><b-col> <i class="fa-solid fa-circle" :class="onlineState"></i></b-col></b-row>
        <b-row> 
            <b-col v-for="item in device.shockers" :key="item.id" class="shocker-col">
                <own-shocker :shocker="item"></own-shocker>
            </b-col>
        </b-row>
    </b-container>
</template>

<script>
import OwnShocker from './OwnShocker.vue'
export default {
    components: { OwnShocker },
    props: ["device"],
    computed: {
        onlineState() {
            if(this.$store.state.deviceStates[this.device.id] === undefined) return 'offline'
            return this.$store.state.deviceStates[this.device.id] ? 'online' : 'offline';
        }
    }
}
</script>

<style scoped lang="scss">
.device {
    border: solid var(--main-seperator-color) 1px;
    border-radius: 10px;
    padding: 20px;

    .shocker-col {
        min-width: 300px;
    }

    .online {
        color: greenyellow;
    }

    .offline {
        color: red;
    }

}

</style>