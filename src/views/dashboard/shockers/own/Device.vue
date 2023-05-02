<template>
    <b-container class="device">
        <b-row>
            <b-col cols="auto">
                <p>{{ device.name }}</p>
            </b-col>
            <b-col :class="onlineStateComp">
                <i class="fa-solid fa-circle"></i>
            </b-col>
        </b-row>
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
    data() {
        return {
            onlineState: false
        }
    },
    beforeMount() {
        this.onlineState = this.getOnlineState();
        this.emitter.on('deviceStateUpdate', () => {
            this.onlineState = this.getOnlineState();
        });
    },
    methods: {
        getOnlineState() {
            if (this.$store.state.deviceStates[this.device.id] === undefined) return false;
            return this.$store.state.deviceStates[this.device.id];
        }
    },
    computed: {
        onlineStateComp() {
            return this.onlineState ? 'online' : 'offline';
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
        @media screen and (min-width: 465px) {
            min-width: 375px;
        }
        
    }

    .online {
        color: greenyellow;
    }

    .offline {
        color: red;
    }

}
</style>