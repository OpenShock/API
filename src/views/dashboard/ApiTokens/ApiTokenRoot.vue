<template>
    <div class="base-wrap">
        <div class="add-circle" @click="openNewModal">
            <i class="fa-solid fa-plus"></i>
        </div>

        <b-container>
            <b-table hover striped :items="tokens" :fields="fields" class="tokens-table">

                <template #cell(permissions)="row">
                    <i class="perm fa-solid fa-bolt" :class="row.item.permissions.includes(0) ? 'enabled' : 'disabled'"></i>
                </template>

                <template #cell(actions)="row">
                    <div cols="auto" class="elli" @click="ellipsis($event, row.item)">
                        <i class="fa-solid fa-ellipsis-vertical"></i>
                    </div>
                </template>

                <template #cell(time)="row">
                    <span>{{ new Date(row.item.createdOn).toLocaleString() }} </span>
                </template>

                <template #cell(validUntil)="row">
                    <span>{{ row.item.validUntil === null ? '' : new Date(row.item.validUntil).toLocaleDateString() }} </span>
                </template>
            </b-table>
        </b-container>

        <b-modal v-model="newTokenModal" title="New Token" ok-title="Create" @ok.prevent="createNewCode">

            <b-container style="padding: 0;">
                <b-form-group label="Name" label-for="item-id" label-class="mb-1">
                    <b-form-input id="item-id" v-model="newToken.name" />
                </b-form-group>


                <BFormCheckbox v-model="newToken.indef" switch>No expiry</BFormCheckbox>
                <b-row v-if="!newToken.indef">
                    <b-form-group label="Valid until" label-for="valid-id" label-class="mb-1">
                        <b-form-input type="date" id="valid-id" v-model="newToken.validUntil" />
                    </b-form-group>
                </b-row>
            </b-container>

        </b-modal>

        <b-modal v-model="editModal" title="Edit Token" ok-title="Save" @ok.prevent="applyEdits">

            <b-container style="padding: 0;">
                <b-form-group label="ID (readonly)" label-for="item-id" label-class="mb-1">
                    <b-form-input id="item-id" v-model="editing.token.id" readonly />
                </b-form-group>
            </b-container>

        </b-modal>
    </div>
</template>

<script>
export default {
    data() {
        return {
            fields: [
                {
                    key: "name"
                },
                {
                    key: "permissions"
                },
                {
                    key: "time",
                    label: "Created On"
                },
                {
                    key: "validUntil",
                    label: "Valid Until"
                },
                {
                    key: 'actions',
                    label: '',
                    thClass: "actions-header"
                }
            ],
            tokens: [],
            editModal: false,
            newTokenModal: false,
            editing: {
                token: {
                    name: "",
                    permissions: []
                }
            },
            newToken: {
                name: "New API Token",
                permissions: [],
                validUntil: Date.UTC(),
                indef: true
            }
        }
    },
    beforeMount() {
        this.load();
    },
    methods: {
        openNewModal() {
            this.newTokenModal = true;

            this.newToken = {
                name: "New API Token",
                permissions: [],
                validUntil: Date.UTC(),
                indef: true
            };
        },
        onContext(ctx) {
            this.newToken.context = ctx;
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
                            this.edit(item);
                        }
                    },
                    {
                        label: "Delete",
                        icon: 'fa-solid fa-trash',
                        onClick: () => {
                            this.remove(item);
                        }
                    }
                ]
            });
        },
        async load() {
            const res = await apiCall.makeCall('GET', `1/tokens`);
            if (res === undefined || res.status !== 200) {
                toastr.error("Error while retrieving API tokens");
                return;
            }

            this.tokens = res.data.data;
        },
        remove(item) {
            this.$swal({
                title: 'Delete?',
                html: `Delete API token <b>${item.name}<br>[${item.id}]</b>
                    <br><br>Are you sure?`,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#d33',
                cancelButtonColor: 'var(--secondary-seperator-color)',
                showLoaderOnConfirm: true,
                confirmButtonText: 'Delete',
                allowOutsideClick: () => !this.$swal.isLoading(),
                preConfirm: async () => {
                    try {
                        const res = await apiCall.makeCall('DELETE', `1/tokens/${item.id}`);
                        if (res.status !== 200) {
                            throw new Error(res);
                        }

                    } catch (err) {
                        this.$swal.showValidationMessage(`Request failed: ${utils.getError(err)}`)
                    }
                },
            }).then(async (result) => {
                if (result.isConfirmed) {
                    this.$swal('Success!', 'Successfully deleted API token', 'success');
                    this.load();
                }
            });
        },
        edit(share) {
            this.editing = share;
            this.editModal = true;
        },
        async applyEdits(item) {
            const res = await apiCall.makeCall('PATCH', `1/tokens/${item.id}`, {
                name: this.editing.token.name,

            });
            if (res === undefined || res.status !== 200) {
                this.$swal('Error', 'Error while updating new token', 'error');
                return;
            }

            this.load();
            this.$swal('Successfully updating API token.', 'success');
        },
        async createNewCode() {
            const res = await apiCall.makeCall('POST', `1/tokens`, {
                name: this.newToken.name,
                validUntil: this.newToken.indef ? null : this.newToken.validUntil
            });
            if (res === undefined || res.status !== 200) {
                this.$swal('Error', 'Error while adding new token', 'error');
                return;
            }

            this.load();
            this.newTokenModal = false;
            this.$swal('Successfully created API token!', `Make sure to save it somewhere secure, as it will not be showen to you again.<br><br>Code: ${res.data.data}`, 'success');
        }
    }
}
</script>

<style scoped lang="scss">
.breadcrum {
    color: #9e9e9e;
    font-size: 14px;
}

:deep(.actions-header) {
    width: 0px;
}

.tokens-table {
    .mr {
        margin-right: 10px;

        --bs-btn-color: #fff;
        --bs-btn-hover-color: #fff;
        --bs-btn-active-color: #fff;
    }

    :deep(td) {
        vertical-align: middle;
    }


    .perm {
        margin-right: 8%;

        &.enabled {
            color: greenyellow;
        }

        &.disabled {
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