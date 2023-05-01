<template>
    <div class="base-wrap">
        <div class="add-circle" @click="createNewCode">
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
            </b-table>
        </b-container>

        <b-modal v-model="editModal" title="Edit Token" ok-title="Save" @ok.prevent="applyEdits">

            <b-container style="padding: 0;">

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
                    key: "id",
                    label: "Token Id"
                },
                {
                    key: "name"
                },
                {
                    key: "permissions"
                },
                {
                    key: 'actions',
                    label: '',
                    thClass: "actions-header"
                }
            ],
            tokens: [],
            editModal: false,
            editing: {
                token: {
                    id: "",
                    name: ""
                }
            }
        }
    },
    beforeMount() {
        this.load();
    },
    methods: {
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
                html: `Delete API token <b>${item.id}</b>
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
        applyEdits() {

        },
        async createNewCode() {
            const res = await apiCall.makeCall('POST', `1/tokens`, {
                name: "",
                validUntil: undefined
            });
            if (res === undefined || res.status !== 200) {
                toastr.error("Error while creating token");
                return;
            }

            this.load();
            this.$swal('Successfully created API token!<br>Make sure to save it somewhere secure, as it will not be showen to you again.', `Code: ${res.data.data}`, 'success');
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