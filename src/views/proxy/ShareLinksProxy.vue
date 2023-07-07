<template>
    <div class="proxy-container">
        <transition mode="out-in" name="component-fade">
            <loading-with-text v-if="loading">Checking login status...</loading-with-text>
            <b-container v-else-if="!loggedIn || true" class="inner-card basic-card">
                <b-row style="margin-bottom: 10px;">
                    <b-button variant="outline-light" @click="login">Login with ShockLink Account</b-button>
                </b-row>
                <b-row>
                    <b-button variant="outline-light" @click="guest">Enter as Guest</b-button>
                </b-row>
            </b-container>
            <div v-else>Loading sharelink interface...</div>
        </transition>
    </div>
</template>

<script>
import LoadingWithText from '../utils/LoadingWithText.vue';

export default {
    components: { LoadingWithText },
    data() {
        return {
            loading: true,
            loggedIn: false
        }
    },
    async beforeMount() {
        await this.checkIfLoggedIn();
    },
    methods: {
        async checkIfLoggedIn() {
            this.loading = true;
            const res = await apiCall.makeCall('GET', '1/users/self');
            if (res === undefined || res.status !== 200) {
                toastr.error("Error while retrieving user information");
                this.loading = false;
                return;
            }

            this.loggedIn = true;
            this.loading = false;
        },
        guest() {

        },
        login() {
            this.$store.dispatch('setReturnUrl', "/dashboard/shares/links/" + this.linkId);
            this.$router.push("/account/login");
        }
    },
    computed: {
        linkId() {
            return this.$route.params.id;
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
    }
}
</style>
