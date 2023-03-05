<template>
    <b-container class="shocker">
        <b-row>
            <p>{{ shocker.name }}</p>
        </b-row>
        <b-row>
            <label for="intensity">Intensity: {{ shocker.state.intensity }}</label>
            <b-form-input id="intensity" type="range" min="1" max="100" v-model="shocker.state.intensity"></b-form-input>
            <label for="duration">Duration: {{ shocker.state.duration / 1000 }}</label>
            <b-form-input step="100" id="duration" type="range" min="300" max="30000" v-model="shocker.state.duration"></b-form-input>
        </b-row>
        <b-row align-h="center">
            <b-col cols="auto" md="auto">
                <loading-button text="Sound" icon="fa-solid fa-volume-high" :disabled="inProgress" loadingIcon="fa-solid fa-spinner fa-spin" :loading="inProgress" @click="control(3)"/>
            </b-col>
            <b-col cols="auto" md="auto">
                <loading-button text="Vibrate" icon="fa-solid fa-water" :disabled="inProgress" loadingIcon="fa-solid fa-spinner fa-spin" :loading="inProgress" @click="control(2)"/>
            </b-col>
            <b-col cols="auto" md="auto">
                <loading-button style="left: 0;" text="Shock" icon="fa-solid fa-bolt" :disabled="inProgress" loadingIcon="fa-solid fa-spinner fa-spin" :loading="inProgress" @click="control(1)"/>
            </b-col>
        </b-row>
    </b-container>
</template>

<script>
import LoadingButton from '../../../utils/LoadingButton.vue';
    export default {
  components: { LoadingButton },

        props: ["shocker"],
        data() {
            return {
                inProgress: false
            }
        },
        methods: {
            control(type) {
                const obj = {
                    "RequestType": 0,
                    "data": [
                        {
                            "Id": this.shocker.id,
                            "Type": type,
                            "Duration": parseInt(this.shocker.state.duration),
                            "Intensity": parseInt(this.shocker.state.intensity)
                        }
                    ]
                };
                console.log(this);
                ws.send(JSON.stringify(obj));
                this.inProgress = true;

                setTimeout(() => this.inProgress = false, this.shocker.state.duration);
            }
        }
    }
</script>

<style scoped lang="scss">
.shocker {
    border: solid var(--main-seperator-color) 1px;
    border-radius: 10px;
    margin: 10px 0;
    padding: 10px;

    .row {
        padding: 0 12px;

        .form-range {
            padding: 0 12px;
        }
    }
}
</style>