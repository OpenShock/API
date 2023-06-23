<template>
    <div>
        <p>Connect via serial to your ESP 32.</p>
        <b-button @click="connect">Connect Serial</b-button>
    </div>
</template>
<script>
export default ({
    methods: {
        connect() {
            this.emitter.emit("setup-serialConnect");
        }
    }
})
</script>
