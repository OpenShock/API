<template>
    <div>
        <b-container class="shareLinks-container">
            <b-row>
                <b-col v-for="item in shareLinks" :key="item.id" class="basic-card sharelink-card" @click.self="$router.push('/dashboard/shares/links/' + item.id)">
                    <b-row>
                        <h3>{{ item.name }}</h3>
                        <b-row align-v="end" class="copy-clip">
                            <b-col align-h="center">
                                <div class="backdrop" title="Copy share link" @click="copyUrl(item.id)">
                                    <i class="fa-solid fa-copy"></i>
                                </div>
                            </b-col>
                        </b-row>
                    </b-row>
                </b-col>
                <b-col class="basic-card sharelink-card add-new">
                    <b-row>
                        <h3 style="margin-bottom: 40px;">Add new share link!</h3>
                        <i class="fa-solid fa-plus"></i>
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
        copyUrl(id) {
            navigator.clipboard.writeText(config.webUiUrl + 'proxy/shares/links/' + id);
            toastr.success('Share url copied to clipboard');
        },
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
    padding-top: 20px;
    margin: 20px;
    cursor: pointer;
    transition: ease-in-out 0.2s background-color;

    box-shadow: rgba(0, 0, 0, 0.24) 0px 3px 8px;

    .copy-clip {
        font-size: 30px;
        padding: 0;
        margin: 0;
        border-radius: 15px;

        .backdrop {

            margin: auto;
            width: 70px;
            height: 70px;
            padding: 15px;
            border-radius: 15px;

            &:hover {
                background-color: var(--main-background-color);
            }


            svg {
                margin: auto;
                width: 40px;
                height: 40px;
            }
        }
    }

    &.add-new {
        background-color: var(--main-blackground-dark);

        svg {
            padding: 0;
            font-size: 50px;
        }
    }

    &:hover {
        background-color: var(--main-seperator-color);
    }
}
</style>