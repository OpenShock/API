<template>
    <div>
        <div class="add-circle" @click="newLink">
            <i class="fa-solid fa-plus"></i>
        </div>

        <b-container>
            <b-row v-for="item in shared" :key="item.id">
                <user :user="item"></user>
            </b-row>
        </b-container>

    </div>
</template>

<script>

import User from './User.vue';
export default {
    components: { User },
    data() {
        return {
            shared: []
        }
    },
    async beforeMount() {
        await this.loadShared();
        this.emitter.on('refreshShockers', async () => {
            await this.loadShockers();
        });
    },
    methods: {
        async loadShared() {
            const res = await apiCall.makeCall('GET', '1/shockers/shared');
            if (res === undefined || res.status !== 200) {
                toastr.error("Error while retrieving shared shockers");
                return;
            }

            this.shared = res.data.data;
            this.shared.forEach(user => {
                user.devices.forEach(device => {
                    device.shockers.forEach(shocker => {
                        shocker.state = {
                            intensity: 25,
                            duration: 1
                        }
                    });
                });
            });

        },
        newLink() {
            this.$swal({
                title: 'Enter shocker share code',
                input: 'text',
                inputAttributes: {
                    autocapitalize: 'off'
                },
                showCancelButton: true,
                confirmButtonText: 'Link',
                showLoaderOnConfirm: true,
                preConfirm: async (id) => {
                    try {
                        const res = await apiCall.makeCall('POST', `1/shares/code/${id}`);
                        if (res.status !== 200) {
                            throw new Error(res.statusText);
                        }
                    } catch (err) {
                        this.$swal.showValidationMessage(`Request failed: ${utils.getError(err)}`)
                    }
                },
                allowOutsideClick: () => !this.$swal.isLoading()
            }).then((result) => {
                if (result.isConfirmed) {
                    this.$swal('Success!', 'Successfully linked shocker', 'success');
                    this.loadShared();
                }
            })
        }
    }
}
</script>

<style scoped lang="scss">
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