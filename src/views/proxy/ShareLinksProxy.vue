<template>
    <div class="proxy-container">
        <transition mode="out-in" name="component-fade">
            <loading-with-text v-if="loading">Checking login status...</loading-with-text>
            <b-container v-else-if="!loggedIn" class="inner-card basic-card">
                <b-form-group class="form-group" v-if="chooseCustomName" id="fieldset-1" label="Choose a name" label-for="input-1"
                    :state="customNameState" invalid-feedback="Please enter a name for yourself">
                    <b-form-input id="input-1" @keyup.enter="enterAsGuest" :state="customNameState" v-model="customName" trim></b-form-input>
                    <b-row class="enter-button">
                        <b-button v-if="customNameState" @click="enterAsGuest" variant="outline-light">Enter</b-button>
                    </b-row>
                </b-form-group>
                <div v-else>
                    <b-row style="margin-bottom: 10px;">
                        <b-button variant="outline-light" @click="login">Login with ShockLink Account</b-button>
                    </b-row>
                    <b-row>
                        <b-button variant="outline-light" @click="guest">Enter as Guest</b-button>
                    </b-row>
                </div>
            </b-container>
            <loading-with-text v-else>Loading sharelink interface...</loading-with-text>
        </transition>
    </div>
</template>

<script>
import LoadingWithText from '../utils/LoadingWithText.vue';

export default {
    components: { LoadingWithText },
    props: ['id'],
    data() {
        return {
            loading: true,
            loggedIn: false,
            chooseCustomName: false,
            customName: ""
        }
    },
    async beforeMount() {
        await this.checkIfLoggedIn();
    },
    methods: {
        async checkIfLoggedIn() {
            this.loading = true;
            this.loggedIn = await utils.checkIfLoggedIn();
            this.loading = false;

            if(this.loggedIn) this.$router.push("/dashboard/shares/links/" + this.id);
        },
        guest() {
            this.chooseCustomName = true;
        },
        enterAsGuest() {
            if(!this.customNameState) return;
            this.$store.commit('setCustomName', this.customName);
            this.$router.push("/public/shares/links/" + this.id);
        },
        login() {
            this.$store.dispatch('setReturnUrl', "/dashboard/shares/links/" + this.id);
            this.$router.push("/account/login");
        }
    },
    computed: {
        customNameState() {
            return this.customName.length >= 3;
        }
    }
}
</script>

<style scoped lang="scss">
.proxy-container {
    display: grid;
    place-items: center;
    min-height: 100vh;

    .inner-card {   
        width: 400px;
        height: 150px;
        padding: 30px;

        .form-group {
            margin-top: -17px;
        }

        .enter-button {
            margin-top: 10px;
            margin-left: 0;
            margin-right: 0;
        }
    }
}
</style>
