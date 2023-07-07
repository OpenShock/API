<template>
    <div>
        <b-container class="shareLinks-container">
            <b-row>
                <b-col v-for="item in shareLinks" :key="item.id" class="basic-card sharelink-card">
                    <b-row>
                        <h3>{{ item.name }}</h3>
                    </b-row>
                </b-col>
            </b-row>
        </b-container>
    </div>
</template>

<script>

export default {
    data() {
        return {
            shareLinks: []
        }
    },
    async beforeMount() {
        await this.loadShareLinks();
    },
    methods: {
        async loadShareLinks() {
            const res = await apiCall.makeCall('GET', `1/shares/links`);
            if (res === undefined || res.status !== 200) {
                toastr.error("Error while retrieving shocker share codes");
                return;
            }

            this.shareLinks = res.data.data;
        }
    }
}
</script>

<style scoped lang="scss">
.sharelink-card {
    max-width: 200px;
    height: 300px;
    text-align: center;
    padding: 10px;

    box-shadow: rgba(0, 0, 0, 0.24) 0px 3px 8px;
}
</style>