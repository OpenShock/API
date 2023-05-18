<template>
    <b-container class="shocker">
        <b-row class="head">
            <b-col cols="10" class="shocker-name-col">
                <p class="shocker-name">{{ shocker.name }}</p>
            </b-col>
            <b-col cols="auto" class="elli" @click="ellipsis">
                <i class="fa-solid fa-ellipsis-vertical"></i>
            </b-col>
        </b-row>
        <b-row>
            <b-container align-items="center" style="margin-top: 15px">
                <b-row align-h="center">
                    <b-col md="auto" style="width: unset">
                        <round-slider
                            v-model="shocker.state.intensity"
                            pathColor="#1b1d1e"
                            rangeColor="#8577ef"
                            start-angle="315"
                            end-angle="+270"
                            width="30"
                            line-cap="round"
                            radius="75"
                            />

                        <p style="text-align: center;">Intensity</p>
                    </b-col>
                    <b-col md="auto"  style="width: unset">
                        <round-slider
                            v-model="shocker.state.duration"
                            pathColor="#1b1d1e"
                            rangeColor="#8577ef"
                            start-angle="315"
                            end-angle="+270"
                            line-cap="round"
                            radius="75"
                            width="30"
                            min="0.3"
                            max="30"
                            step="0.1"
                            />

                        <p style="text-align: center;">Duration</p>
                    </b-col>
                </b-row>
            </b-container>
        </b-row>
        <b-row align-h="center">
            <b-col cols="auto" md="auto">
                <loading-button style="width: 46px" text="" icon="fa-solid fa-volume-high" :disabled="inProgress" loadingIcon="fa-solid fa-spinner fa-spin" :loading="inProgress" @click="control(3)"/>
            </b-col>
            <b-col cols="auto" md="auto">
                <loading-button style="width: 46px" text="" icon="fa-solid fa-water" :disabled="inProgress" loadingIcon="fa-solid fa-spinner fa-spin" :loading="inProgress" @click="control(2)"/>
            </b-col>
            <b-col cols="auto" md="auto">
                <loading-button style="left: 0; width: 46px" text="" icon="fa-solid fa-bolt" :disabled="inProgress" loadingIcon="fa-solid fa-spinner fa-spin" :loading="inProgress" @click="control(1)"/>
            </b-col>
        </b-row>
    </b-container>
</template>

<script>
import Loading from '../../../utils/Loading.vue';
import LoadingButton from '../../../utils/LoadingButton.vue';
import RoundSlider from 'vue-three-round-slider';

export default {
  components: { LoadingButton, Loading, RoundSlider },

        props: ["shocker"],
        data() {
            return {
                inProgress: false
            }
        },
        beforeMount() {

        },
        methods: {
            ellipsis(e) {
                this.$contextmenu({
                    theme: utils.isDarkMode() ? 'default dark' : 'default',
                    x: e.x,
                    y: e.y,
                    items: [
                        { 
                            label: "Remove share",
                            icon: 'fa-solid fa-trash',
                            onClick: () => {
                                this.delete();
                            }
                        }
                    ]
                }); 
            },
            delete() {
                this.$swal({
                    title: 'Unlink share?',
                    html: `You are remove the share link for shocker <b>${this.shocker.name}</b> with id (${this.shocker.id}).<br>You wont be able to control the shocker after unlinking it.</b>
                    <br><br>Are you sure?`,
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonColor: '#d33',
                    cancelButtonColor: 'var(--secondary-seperator-color)',
                    showLoaderOnConfirm: true,
                    confirmButtonText: 'Unlink shared shocker',
                    allowOutsideClick: () => !this.$swal.isLoading(),
                    preConfirm: async () => {
                        try {
                        const res = await apiCall.makeCall('DELETE', `1/shares/${this.shocker.id}`);
                        if (res.status !== 200) {
                            throw new Error(res.statusText);
                        }

                        } catch (err) {
                        this.$swal.showValidationMessage(`Request failed: ${err}`)
                        }
                    },
                    }).then(async (result) => {
                        if (result.isConfirmed) {
                            this.$swal('Success!', 'Successfully unlinked shared shocker', 'success');
                            this.emitter.emit('refreshShockers');
                        }
                });
            },
            control(type) {
                const obj = {
                    "RequestType": 0,
                    "data": [
                        {
                            "Id": this.shocker.id,
                            "Type": type,
                            "Duration": parseInt(this.shocker.state.duration) * 1000,
                            "Intensity": parseInt(this.shocker.state.intensity)
                        }
                    ]
                };
                ws.send(JSON.stringify(obj));
                this.inProgress = true;

                setTimeout(() => this.inProgress = false, this.shocker.state.duration);
            }
        }
    }
</script>

<style scoped lang="scss">
.shocker {
    background-color: var(--secondary-background-color);
    border: solid var(--main-seperator-color) 1px;
    border-radius: 10px;
    margin: 10px 0;
    padding: 10px;

    .head {
        border-bottom: solid 2px var(--main-background-color);
        padding-bottom: 10px !important;

        .shocker-name-col {
            margin-right: auto;
            width: calc(100% - 24px);

            .shocker-name {
                white-space: nowrap;
                text-overflow: ellipsis;
                overflow: hidden;
                margin-bottom: 0;
            }
        }   
    }


    .row {
        padding: 0 12px;

        .form-range {
            padding: 0 12px;
        }
    }
}
</style>