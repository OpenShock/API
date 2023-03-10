<template>
    <b-container class="device">
        <b-row><p>{{ device.name }}</p></b-row>
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
    props: ["device"]
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

}

</style>