<template>
    <div class="base-wrap">
        <b-container>
            <div v-if="page === 0">
                <h>Setup assistant</h>
                <p>Prepare your esp, install the firmware, and make have a device to connect to its wifi ready.</p>
                <button @click="page++">Next</button>
            </div>
            <div v-if="page === 1">
                <h>Connect to esp's wifi</h>
                <button @click="page++">Next</button>
            </div>
            <div v-if="page === 2">
                <h>Enter wifi credentials</h>
                <b-table hover striped :items="networks" :fields="fields" class="network-table">
                    <template #cell(ssid)="row">
                        <b-form-input v-model="row.item.ssid"></b-form-input>
                    </template>

                    <template #cell(password)="row">
                        <b-form-input v-model="row.item.password"></b-form-input>
                    </template>

                    <template #cell(actions)="row">
                        <div cols="auto" class="elli" @click="removeNetworks(row.item)">
                            <i class="fa-solid fa-trash"></i>
                        </div>
                    </template>
                </b-table>

                <b-button @click="espSetWifi">Save & Try to connect</b-button>

            </div>
        </b-container>
    </div>
</template>
  
<script>
import Loading from '../../../utils/Loading.vue';
import axios from 'axios';

export default {
    components: { Loading },
    data() {
        return {
            page: 0,
            fields: [
                {
                    key: "ssid"
                },
                {
                    key: "password"
                },
                {
                    key: 'actions',
                    thClass: "actions-header",
                    label: "",
                    tdClass: "action-header-td"
                }
            ],
            device: undefined,
            networks: [
                {
                    ssid: "yes",
                    password: "aaa"
                }
            ]
        }
    },
    mounted() {
        this.$store.dispatch('setNewNav', []);
    },
    async beforeMount() {
        await this.loadDevice();
    },
    methods: {
        async loadDevice() {
            const res = await apiCall.makeCall('GET', '1/devices/' + this.$route.params.id);
            if (res === undefined || res.status !== 200) {
                toastr.error("Error while retrieving device details");
                return;
            }

            this.device = res.data.data;
        },
        async espSetWifi() {
            const res = await axios({
                method: "SEND",
                url: "http://10.10.10.10/networks",
                data: "Luc-H,LucNetworkPw12"
            });
            console.log(res);
        },
        removeNetworks(item) {
            var index = this.networks.indexOf(item);
            if (index !== -1) {
                this.networks.splice(index, 1);
            }
        }
    }
}
</script>
  
<style scoped lang="scss">
:deep(.action-header-td) {
    display: table-cell;
    vertical-align: middle;
}

:deep(.actions-header) {
    width: 0px;
}

.network-table {
    .mr {
        margin-right: 10px;

        --bs-btn-color: #fff;
        --bs-btn-hover-color: #fff;
        --bs-btn-active-color: #fff;
    }
}
</style>