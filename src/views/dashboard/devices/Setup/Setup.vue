<template>
    <div class="base-wrap">
        <b-container>
            <b-row align-h="center">
                <b-col md="auto">
                    <h3>ESP-32 setup assistant</h3>
                </b-col>
            </b-row>
            <serial-flash></serial-flash>
        </b-container>
    </div>
</template>
  
<script>
import Loading from '../../../utils/Loading.vue';
import { connect } from './Lib/Ada.js'
import Page1 from './Page1';
import Page2 from './Page2';
import SerialFlash from './SerialFlash.vue';

export default {
    components: { Loading, Page1, Page2, SerialFlash },
    data() {
        return {
            page: 1,
            device: undefined,
            networks: [
                {
                    ssid: "yes",
                    password: "aaa"
                }
            ],
            espStub: undefined
        }
    },
    mounted() {
        this.$store.dispatch('setNewNav', []);
        this.emitter.on("setup-serialConnect", () => {
            this.serialConnect();
        });
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

        logMsg(text) {
            console.log(text);
        },
        async serialConnect() {
            if (this.espStub !== undefined) {
                await this.espStub.disconnect();
                await this.espStub.port.close();
                this.espStub = undefined;
            }
            const esploader = await connect({
                log: (...args) => this.logMsg(...args),
                debug: (...args) => this.logMsg(...args),
                error: (...args) => this.logMsg(...args),
            })

            await esploader.initialize();

            this.logMsg("Connected to " + esploader.chipName);
            this.logMsg("MAC Address: " + this.formatMacAddr(esploader.macAddr()));

            this.espStub = await esploader.runStub();
        },
        async changeBaudRate() {
            if (this.espStub !== undefined) {
                await this.espStub.setBaudrate(921600);
            }
        },
        async erase() {
            if (this.espStub !== undefined) {
                this.logMsg("Erasing flash memory. Please wait...");
                let stamp = Date.now();
                await this.espStub.eraseFlash();
                this.logMsg("Finished. Took " + (Date.now() - stamp) + "ms to erase.");
            }
        },
        async flashData() {
            this.logMsg("Downloading...");
            fetch('https://cdn.shocklink.net/firmware/shocklink_firmware_0.5.2.0.bin').then(res => res.arrayBuffer()).then(async buff => {
                this.logMsg("Flashing memory. Please wait...");
                let stamp = Date.now();
                await this.espStub.flashData(
                    buff,
                    (bytesWritten, totalBytes) => {

                    },
                    0,
                    true
                );
                this.logMsg("Finished. Took " + (Date.now() - stamp) + "ms to flash.");
            });

        },
        async hardReset() {
            await this.espStub.hardReset();
        },

        formatMacAddr(macAddr) {
            return macAddr.map((value) => value.toString(16).toUpperCase().padStart(2, "0")).join(":");
        }
    },
    computed: {
        supported() {
            return ('serial' in navigator)
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