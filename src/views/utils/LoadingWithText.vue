<template>
    <div v-if="loading" class="loading-text">
        <loading/>
        <slot></slot>
    </div>
</template>

<script>
import Loading from '@/views/utils/Loading';

export default {
    components: { Loading },
    props: {
        loading: {
            type: Boolean,
            default: true
        }
    }
}
</script>

<style scoped>
.loading-text {
    text-align: center;
}
</style>