<template>
    <b-container class="user">
        <b-row><p>{{ user.name }}</p></b-row>
        <b-row> 
            <b-col v-for="item in user.shockers" :key="item.id" class="shocker-col">
                <shared-shocker :shocker="item"></shared-shocker>
            </b-col>
        </b-row>
    </b-container>
</template>

<script>
import SharedShocker from './SharedShocker.vue'
export default {
    components: {SharedShocker},
    props: ["user"]
}

</script>

<style scoped lang="scss">
.user {
    border: solid var(--main-seperator-color) 1px;
    border-radius: 10px;
    padding: 20px;

    .shocker-col {
        min-width: 300px;
    }

}

</style>