<template>
    <div>
        <b-container class="shareLinks-container">
            <b-row>
                <b-col v-for="item in shareLinks" :key="item.id" class="basic-card selectable sharelink-card"
                    @click.self="$router.push('/dashboard/shares/links/' + item.id)">
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
                <b-col class="basic-card sharelink-card selectable add-new" @click="newShareLink.modal = true">
                    <b-row>
                        <h3 style="margin-bottom: 40px;">Add new share link!</h3>
                        <i class="fa-solid fa-plus"></i>
                    </b-row>
                </b-col>
            </b-row>
        </b-container>

        <b-modal v-model="newShareLink.modal" title="New Share Link" ok-title="Create" @ok.prevent="addNewShareLink">

            <b-container style="padding: 0;">
                <b-form-group label="Name" label-for="item-id" label-class="mb-1">
                    <b-form-input id="item-id" v-model="newShareLink.name" />
                </b-form-group>

                <BFormCheckbox v-model="newShareLink.indef" switch>No expiry</BFormCheckbox>
                <b-row v-if="!newShareLink.indef">
                    <b-form-group label="Valid until" label-for="valid-id" label-class="mb-1">
                        <b-form-input type="date" id="valid-id" v-model="newShareLink.validUntil" />
                    </b-form-group>
                </b-row>
            </b-container>

        </b-modal>
    </div>
</template>

<script>

export default {
    data() {
        return {
            shareLinks: [],
            newShareLink: {
                modal: false,
                name: "Share Link",
                permissions: [],
                validUntil: Date.UTC(),
                indef: true
            }
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
                toastr.error("Error while loading Share Links");
                return;
            }

            this.shareLinks = res.data.data;
        },
        async addNewShareLink() {
            this.newShareLink.modal = true;

            const res = await apiCall.makeCall("POST", "1/shares/links", {
                name: this.newShareLink.name,
                validUntil: this.newShareLink.indef ? null : this.newShareLink.validUntil
            });

            if (res === undefined || res.status !== 200) {
                this.$swal('Error', 'Error while adding new Share Link', 'error');
                return;
            }

            this.loadShareLinks();
            this.newShareLink.modal = false;
            this.$swal('Successfully created Share Link!', '', 'success');
        }
    }
}
</script>

<style scoped lang="scss">
.sharelink-card {
    max-width: 224px;
    min-width: 224px;
    height: 300px;
    text-align: center;
    padding: 10px;
    padding-top: 20px;
    margin: 20px;
    overflow-wrap: anywhere;

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
            transition: ease-in-out 0.2s background-color;

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
}
</style>