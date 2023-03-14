<template>
    <div v-if="loading" class="loading-text">
        <loading/>
        <div v-html="text"/>
    </div>
</template>

<script>
import Loading from '@/views/utils/Loading';

export default {
    components: { Loading },
    props: ['loading', 'text']
}
</script>

<style scoped>
.loading-text {
    text-align: center;
}
</style>