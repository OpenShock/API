<template>
    <div>
        <div class="add-circle" @click="createNewCode">
            <i class="fa-solid fa-plus"></i>
        </div>

        <b-container>
            <b-table hover striped :items="shares" :fields="fields" class="shares-table">

                <template #cell(pause)="row">
                    <b-col cols="auto" class="pause-col" @click="togglePause(row.item)">
                        <span v-if="row.item.paused" class="paused">
                            <i class="fa-solid fa-play"></i>
                        </span>

                        <span v-else>
                            <i class="fa-solid fa-pause"></i>
                        </span>
                    </b-col>
                </template>

                <template #cell(shared-with)="row">
                    <b-container>
                        <b-row align-h="start" align-v="center">
                            <b-col md="auto">
                                <img class="user-image" :src="row.item.sharedWith.image + 'x128'" />
                            </b-col>
                            <b-col>
                                <p class="mb-0">{{ row.item.sharedWith.name }}</p>
                            </b-col>
                        </b-row>
                    </b-container>
                </template>

                <template #cell(permissions)="row">
                    <span class="perm">
                        <span :class="row.item.permissions.sound ? 'enabled' : 'disabled'">
                            <i class="fa-solid fa-volume-high"></i>
                        </span>

                        <span :class="row.item.permissions.vibrate ? 'enabled' : 'disabled'">
                            <i class="perm fa-solid fa-water"></i>
                        </span>
                        <span :class="row.item.permissions.shock ? 'enabled' : 'disabled'">
                            <i class="perm fa-solid fa-bolt"></i>
                        </span>
                    </span>
                </template>

                <template #cell(limits)="row">
                    <span>I: {{ row.item.limits.intensity }}</span>
                    <span>D: {{ row.item.limits.duration }}</span>
                </template>

                <template #cell(actions)="row">
                    <div cols="auto" class="elli" @click="ellipsis($event, row.item)">
                        <i class="fa-solid fa-ellipsis-vertical"></i>
                    </div>
                </template>
            </b-table>
        </b-container>

        <b-container>
            <b-table hover striped :items="codes" :fields="fieldsCodes" class="share-codes-table">
                <template #cell(actions)="row">
                    <div cols="auto" class="elli" @click="ellipsisCode($event, row.item)">
                        <i class="fa-solid fa-ellipsis-vertical"></i>
                    </div>
                </template>

                <template #cell(createdOn)="row">
                    <span>{{ new Date(row.item.createdOn).toLocaleString() }} </span>
                </template>
            </b-table>
        </b-container>

        <b-modal v-model="editModal" title="Edit Share" ok-title="Save" @ok.prevent="applyEdits">

            <b-container style="padding: 0;">
                <b-row align-h="start" align-v="center">
                    <b-col md="auto">
                        <img class="user-image" :src="editing.sharedWith.image + 'x128'" />
                    </b-col>
                    <b-col>
                        <p class="mb-0">{{ editing.sharedWith.name }}</p>
                    </b-col>
                </b-row>

                <b-row>
                    <b-container align-items="center" style="margin-top: 15px">
                        <b-row align-h="center">
                            <b-col md="auto" style="width: unset">
                                <round-slider v-model="editing.limitsTranslated.intensity" pathColor="#1b1d1e"
                                    rangeColor="#f16051" start-angle="315" end-angle="+270" width="30" line-cap="round"
                                    radius="75" />

                                <p style="text-align: center;">Intensity Limit</p>
                            </b-col>
                            <b-col md="auto" style="width: unset">
                                <round-slider v-model="editing.limitsTranslated.duration" pathColor="#1b1d1e"
                                    rangeColor="#f16051" start-angle="315" end-angle="+270" line-cap="round" radius="75"
                                    width="30" min="0.3" max="30" step="0.1" />

                                <p style="text-align: center;">Duration Limit</p>
                            </b-col>
                        </b-row>
                    </b-container>
                </b-row>

                <b-row align-h="center">
                    <b-col cols="auto" md="auto">
                        <permission-button style="width: 46px" icon="fa-solid fa-volume-high"
                            :state="editing.permissions.sound"
                            @click="editing.permissions.sound = !editing.permissions.sound" />
                    </b-col>
                    <b-col cols="auto" md="auto">
                        <permission-button style="width: 46px" icon="fa-solid fa-water" :state="editing.permissions.vibrate"
                            @click="editing.permissions.vibrate = !editing.permissions.vibrate" />
                    </b-col>
                    <b-col cols="auto" md="auto">
                        <permission-button style="left: 0; width: 46px" icon="fa-solid fa-bolt"
                            :state="editing.permissions.shock"
                            @click="editing.permissions.shock = !editing.permissions.shock" />
                    </b-col>
                </b-row>

            </b-container>

        </b-modal>
    </div>
</template>

<script>
import RoundSlider from 'vue-three-round-slider';
import PermissionButton from '../../../shares/links/edit/PermissionButton.vue';
export default {
    components: { RoundSlider, PermissionButton },
    data() {
        return {
            fields: [
                {
                    key: 'pause',
                    label: '',
                    thClass: "width0"
                },
                {
                    key: "shared-with",
                    label: "Shared With"
                },
                {
                    key: "permissions"
                },
                {
                    key: "limits"
                },
                {
                    key: 'actions',
                    label: '',
                    thClass: "width0"
                }
            ],
            fieldsCodes: [
                {
                    key: "id",
                    label: "Code / Id"
                },
                {
                    key: "createdOn",
                    label: "Created On"
                },
                {
                    key: 'actions',
                    label: '',
                    thClass: "width0"
                }
            ],
            codes: [],
            shares: [],
            editModal: false,
            editing: {
                sharedWith: {
                    id: "",
                    name: "",
                    image: ""
                },
                limitsTranslated: {
                    intensity: 0,
                    duration: 0
                },
                permissions: {
                    shock: false,
                    sound: false,
                    vibrate: false
                }
            }
        }
    },
    beforeMount() {
        this.loadShares();
        this.loadCodes();
    },
    methods: {
        async togglePause(item) {
            const toSet = !item.paused;
            await apiCall.makeCall("POST", `1/shockers/${this.$route.params.id}/shares/${item.sharedWith.id}/pause`, {
                pause: toSet
            });

            item.paused = toSet;
        },
        ellipsisCode(e, item) {
            this.$contextmenu({
                theme: utils.isDarkMode() ? 'default dark' : 'default',
                x: e.x,
                y: e.y,
                items: [
                    {
                        label: "Remove",
                        icon: 'fa-solid fa-trash',
                        onClick: () => {
                            this.removeCode(item);
                        }
                    }
                ]
            });
        },
        ellipsis(e, item) {
            this.$contextmenu({
                theme: utils.isDarkMode() ? 'default dark' : 'default',
                x: e.x,
                y: e.y,
                items: [
                    {
                        label: "Edit",
                        icon: 'fa-solid fa-pen-to-square',
                        onClick: () => {
                            this.editShare(item);
                        }
                    },
                    {
                        label: "Unshare",
                        icon: 'fa-solid fa-trash',
                        onClick: () => {
                            this.removeShare(item);
                        }
                    }
                ]
            });
        },
        async loadCodes() {
            const res = await apiCall.makeCall('GET', `1/shockers/${this.$route.params.id}/shareCodes`);
            if (res === undefined || res.status !== 200) {
                toastr.error("Error while retrieving shocker share codes");
                return;
            }

            this.codes = res.data.data;
        },
        removeCode(code) {
            this.$swal({
                title: 'Remove share code?',
                html: `Remove share code <b>${code.id}?</b>
                    <br><br>Are you sure?`,
                icon: 'question',
                showCancelButton: true,
                confirmButtonColor: '#d33',
                cancelButtonColor: 'var(--secondary-seperator-color)',
                showLoaderOnConfirm: true,
                confirmButtonText: 'Remove share code',
                allowOutsideClick: () => !this.$swal.isLoading(),
                preConfirm: async () => {
                    try {
                        const res = await apiCall.makeCall('DELETE', `1/shares/code/${code.id}`);
                        if (res.status !== 200) {
                            throw new Error(res);
                        }

                    } catch (err) {
                        this.$swal.showValidationMessage(`Request failed: ${utils.getError(err)}`)
                    }
                },
            }).then(async (result) => {
                if (result.isConfirmed) {
                    this.$swal('Success!', 'Successfully remove share code', 'success');
                    this.loadCodes();
                }
            });
        },
        async loadShares() {
            const res = await apiCall.makeCall('GET', `1/shockers/${this.$route.params.id}/shares`);
            if (res === undefined || res.status !== 200) {
                toastr.error("Error while retrieving shocker shares");
                return;
            }

            this.shares = res.data.data;
        },
        removeShare(share) {
            this.$swal({
                title: 'Unshare?',
                html: `Unshare shocker for <b>${share.sharedWith.name}</b>
                    <br><br>Are you sure?`,
                icon: 'question',
                showCancelButton: true,
                confirmButtonColor: '#d33',
                cancelButtonColor: 'var(--secondary-seperator-color)',
                showLoaderOnConfirm: true,
                confirmButtonText: 'Unshare',
                allowOutsideClick: () => !this.$swal.isLoading(),
                preConfirm: async () => {
                    try {
                        const res = await apiCall.makeCall('DELETE', `1/shockers/${this.$route.params.id}/shares/${share.sharedWith.id}`);
                        if (res.status !== 200) {
                            throw new Error(res);
                        }

                    } catch (err) {
                        this.$swal.showValidationMessage(`Request failed: ${utils.getError(err)}`)
                    }
                },
            }).then(async (result) => {
                if (result.isConfirmed) {
                    this.$swal('Success!', 'Successfully unshared shocker', 'success');
                    this.loadShares();
                }
            });
        },
        editShare(share) {
            var temp = share;
            temp.limitsTranslated = {
                duration: temp.limits.duration === null ? 30 : temp.limits.duration / 1000.0,
                intensity: temp.limits.intensity === null ? 100 : temp.limits.intensity
            }

            this.editing = temp;
            this.editModal = true;
        },
        async applyEdits() {
            const temp = this.editing;
            const res = await apiCall.makeCall('PATCH', `1/shockers/${this.$route.params.id}/shares/${temp.sharedWith.id}`, {
                permissions: {
                    vibrate: temp.permissions.vibrate,
                    shock: temp.permissions.shock,
                    sound: temp.permissions.sound
                },
                limits: {
                    duration: temp.limitsTranslated.duration == 30.0 ? null : temp.limitsTranslated.duration * 1000,
                    intensity: temp.limitsTranslated.intensity == 100 ? null : temp.limitsTranslated.intensity
                }
            });
            if (res === undefined || res.status !== 200) {
                toastr.error("Error while creating share code");
                return;
            }

            this.$swal('Success!', 'Successfully updated share', 'success');
            this.loadShares();
            this.editModal = false;
        },
        async createNewCode() {
            const res = await apiCall.makeCall('POST', `1/shockers/${this.$route.params.id}/shares`, {
                permissions: {
                    vibrate: true,
                    shock: true,
                    sound: true
                },
                limits: {
                    duration: null,
                    intensity: null
                }
            });
            if (res === undefined || res.status !== 200) {
                toastr.error("Error while creating share code");
                return;
            }

            this.loadCodes();
            this.$swal('Successfully created share code!', `Code: ${res.data.data}`, 'success');
        }
    }
}
</script>

<style scoped lang="scss">
.breadcrum {
    color: #9e9e9e;
    font-size: 14px;
}

.shares-table {
    .mr {
        margin-right: 10px;

        --bs-btn-color: #fff;
        --bs-btn-hover-color: #fff;
        --bs-btn-active-color: #fff;
    }

    :deep(td) {
        vertical-align: middle;
    }

    .pause-col {
        cursor: pointer;
        padding-left: 4px;
        padding-right: 4px;
        width: 20px;

        .paused {
            color: rgb(255, 86, 86);
        }
    }


    .perm {

        svg {
            margin-right: 8%;
        }

        .enabled svg {
            color: greenyellow;
        }

        .disabled svg {
            color: red;
        }
    }
}

.elli {
    width: 24px;

    .fa-ellipsis-vertical {
        height: 24px;
        margin: auto;
        display: block;
    }
}

.add-circle {
    position: fixed;
    right: 10px;
    bottom: 10px;
    width: 60px;
    height: 60px;


    background-color: #7ac142;
    border-radius: 50%;
    cursor: pointer;

    transition: background-color 0.2s;

    &:hover {
        background-color: #5e9634;
    }

    svg {
        height: 40px;
        width: 40px;

        position: relative;
        left: 52%;
        transform: translateX(-50%) translateY(-50%);
        top: 50%;

    }
}
</style>