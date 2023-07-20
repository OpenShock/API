<template>
    <div v-if="shareLink != null">
        <b-container>
            <b-row>
                <b-col>
                    <h3>{{ this.shareLink.name }}</h3>
                </b-col>
                <b-col>
                    <p>{{ this.shareLink.author.name }}</p>
                </b-col>
                <b-col cols="auto" class="elli" @click="ellipsis">
                    <i class="fa-solid fa-ellipsis-vertical"></i>
                </b-col>
            </b-row>
            <div v-if="editMode">
                <b-row v-for="device in shareLink.devices" :key="device.id">
                    <b-col v-for="item in device.shockers" :key="item.id" class="shocker-col">
                        <share-link-shocker-edit :shocker="item"></share-link-shocker-edit>
                    </b-col>
                </b-row>
            </div>
            <div v-else>
                <b-row v-for="device in shareLink.devices" :key="device.id">
                    <b-col v-for="item in device.shockers" :key="item.id" class="shocker-col">
                        <share-link-shocker :shocker="item" @control="controlShocker"></share-link-shocker>
                    </b-col>
                </b-row>
            </div>
        </b-container>
    </div>

    <b-modal v-model="addShocker.modal" title="Add Shocker" ok-title="Add" @ok.prevent="addShockerAction">
        <loading v-if="addShocker.ownShockersLoading"></loading>
        <div v-else>
            <b-container style="padding: 0;">
                <b-form-group label="Shocker" label-for="shocker">
                    <b-form-select :state="validateAddShocker" id="shocker" v-model="addShocker.selectedShocker"
                        :options="addShockerList" required />
                    <b-form-invalid-feedback :state="validateAddShocker">
                        Select a shocker
                    </b-form-invalid-feedback>
                </b-form-group>
            </b-container>
        </div>
    </b-modal>
</template>

<script>
import ShareLinkShocker from "./ShareLinkShocker.vue"
import ShareLinkShockerEdit from './edit/ShareLinkShockerEdit.vue';
import Loading from '../../../utils/Loading.vue';
import ShareLinkHub from '@/js/ShareLinkHub.js';

export default {
    components: { ShareLinkShocker, ShareLinkShockerEdit, Loading },
    props: ['id'],
    data() {
        return {
            shareLink: undefined,
            editMode: false,
            ownShockers: [],
            addShocker: {
                modal: false,
                ownShockersLoading: true,
                ownShockers: [],
                selectedShocker: undefined
            },
            userHubInstance: new ShareLinkHub(this.id)
        }
    },
    async beforeMount() {
        console.log("Starting Share Link Hub connection");
        this.userHubInstance.start();
        await this.loadShareLink();
        this.emitter.on('refreshShareLink', async () => {
            await this.loadShareLink();
        });
    },
    unmounted() {
        console.log("Shutting down Share Link Hub connection");
        this.userHubInstance.stop();
    },
    methods: {
        async loadShareLink() {
            const res = await apiCall.makeCall("GET", "1/public/shares/links/" + this.id);
            if (res === undefined || res.status !== 200) {
                toastr.error("Error while loading Share Link");
                return;
            }
            var temp = res.data.data;

            temp.devices.forEach(device => {
                device.shockers.forEach(shocker => {
                    shocker.state = {
                        intensity: 25,
                        duration: 1
                    }
                });
            });

            this.shareLink = temp;

        },
        async loadAllOwnShockers() {
            this.addShocker.ownShockersLoading = true;
            const res = await apiCall.makeCall('GET', '1/shockers/own');
            if (res === undefined || res.status !== 200) {
                toastr.error("Error while retrieving own shockers");
                return;
            }

            this.addShocker.ownShockers = res.data.data;
            this.addShocker.ownShockersLoading = false;
        },
        async openAddShockerModal() {
            this.loadAllOwnShockers();
            this.selectedShocker = undefined;
            this.addShocker.loading = true;
            this.addShocker.modal = true;
        },
        async addShockerAction() {
            if (!this.validateAddShocker) return;
            const res = await apiCall.makeCall('POST', `1/shares/links/${this.id}/${this.addShocker.selectedShocker}`);
            if (res === undefined || res.status !== 200) {
                toastr.error("Error while adding shocker to share link");
                return;
            }

            this.loadShareLink();
            this.addShocker.modal = false;
            this.$swal('Successfully added Shocker to Share Link!', '', 'success');
        },
        deleteShareLink() {
            this.$swal({
                title: 'Delete Share Link?',
                html: `You are about to delete Share Link:<br><b>${this.shareLink.name}</b><br><br>
                    This action is permanent and cannot be undone.
                    <br><br>Are you sure?`,
                icon: 'question',
                showCancelButton: true,
                confirmButtonColor: '#d33',
                cancelButtonColor: 'var(--secondary-seperator-color)',
                showLoaderOnConfirm: true,
                confirmButtonText: 'Delete Share Link',
                allowOutsideClick: () => !this.$swal.isLoading(),
                preConfirm: async () => {
                    try {
                        const res = await apiCall.makeCall('DELETE', `1/shares/links/${this.id}`);
                        if (res.status !== 200) {
                            throw new Error(res);
                        }

                    } catch (err) {
                        this.$swal.showValidationMessage(`Request failed: ${utils.getError(err)}`)
                    }
                },
            }).then(async (result) => {
                if (result.isConfirmed) {
                    this.$swal('Success!', 'Successfully deleted Share Link', 'success');
                    this.$router.push('/dashboard/shares/links');
                }
            });
        },
        ellipsis(e) {
            this.$contextmenu({
                theme: utils.isDarkMode() ? 'default dark' : 'default',
                x: e.x,
                y: e.y,
                items: [
                    {
                        label: "Add Shocker",
                        icon: 'fa-solid fa-plus',
                        onClick: () => {
                            this.openAddShockerModal();
                        }
                    },
                    {
                        label: "Edit Mode",
                        icon: this.editMode ? 'fa-solid fa-toggle-on' : 'fa-solid fa-toggle-off',
                        onClick: () => {
                            this.editMode = !this.editMode;
                        }
                    },
                    {
                        label: "Remove",
                        icon: 'fa-solid fa-trash',
                        onClick: () => {
                            this.deleteShareLink();
                        }
                    }
                ]
            });
        },
        controlShocker(controlData) {
            this.userHubInstance.control(controlData.id, controlData.intensity, controlData.duration, controlData.type);
        }
    },
    computed: {
        validateAddShocker() {
            return this.addShocker.selectedShocker !== undefined && this.addShocker.selectedShocker !== "";
        },
        existingShockerIds() {
            var arr = [];
            this.shareLink.shockers.forEach(it => {
                arr.push(it.id);
            });
            return arr;
        },
        addShockerList() {
            var arr = [];
            this.addShocker.ownShockers.forEach(it => {
                it.shockers.forEach(shocker => {
                    if (!this.existingShockerIds.includes(shocker.id)) arr.push({
                        text: shocker.name,
                        value: shocker.id
                    });

                });
            });
            return arr;
        },
    }
}
</script>

<style scoped lang="scss">
.shocker-col {
    @media screen and (min-width: 440px) {
        min-width: 440px;
    }
}
</style>