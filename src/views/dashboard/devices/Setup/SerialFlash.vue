<template>
    <b-container class="main">
        <b-row align-h="center" v-if="!supported">
            <b-col md="auto">
                <p>Your browser is not supported. Use Chrome, Edge or Opera</p>
            </b-col>
        </b-row>
        <div v-if="supported">
            <transition name="component-fade" mode="out-in">
                <div v-if="stage === 'download'">
                    <b-row>
                        <p>{{ download.state }}</p>
                    </b-row>
                    <b-row v-if="!download.started" align-h="center">
                        <b-col md="auto">
                            <b-button @click="downloadFirmware">Download</b-button>
                        </b-col>
                    </b-row>
                    <b-row v-else>
                        <div>
                            <b-progress :value="download.progress" showProgress="true"
                                :variant="download.finished ? 'success' : 'primary'" max=1></b-progress>
                        </div>
                        <div v-if="download.finished && download.success">
                            <b-button @click="stage = 'connect'">Next</b-button>
                        </div>
                    </b-row>
                </div>
                <div v-else-if="stage === 'connect'">
                    <b-row align-h="center">
                        <b-col md="auto">
                            <loading-button @click="serialConnect" :loading="connect.connecting"
                                :text="connect.connecting ? 'Connecting...' : 'Connect to ESP'"
                                :disabled="connect.connecting" />
                        </b-col>
                    </b-row>
                </div>
                <div v-else-if="stage === 'flash'">
                    <b-button @click="readSerialTerminal"></b-button>
                    <b-row>
                        <p>{{ flash.state }}</p>
                    </b-row>
                    <b-row>
                        <div align-h="center" v-if="!flash.started">
                            <b-col md="auto">
                                <b-button @click="flashData">Flash</b-button>
                            </b-col>
                        </div>
                        <div v-else>
                            <b-progress :value="flash.progress" showProgress="true"
                                :variant="flash.finished ? 'success' : 'primary'" max=1></b-progress>
                        </div>
                    </b-row>
                </div>
            </transition>
        </div>
    </b-container>
    <b-container>
        <textarea v-model="logsFormatted"></textarea>
        <textarea v-model="this.text"></textarea>
    </b-container>
</template>
<script>
import ApiCall from '../../../../js/ApiCall.js';
import LoadingButton from '../../../utils/LoadingButton.vue';
import { connect } from './Lib/Ada.js'

export default {
    components: { LoadingButton },
    data() {
        return {
            step: 0,
            logs: [],
            espStub: undefined,
            connected: false,
            stage: 'download',
            firmware: undefined,
            download: {
                started: false,
                finished: false,
                success: false,
                state: "Press download to start firmware download",
                progress: 0
            },
            connect: {
                connecting: false
            },
            flash: {
                started: false,
                finished: false,
                progress: 0,
                state: "Press flash to start flashing your device"
            },
            text: ""
        }
    },
    methods: {
        async processStream(stream) {
            const textDecoder = new TextDecoder();
            while (true) {
                const value = await stream.read();
                if (value === null) {
                    console.log('End of stream');
                    break;
                }

                this.text += textDecoder.decode(value.value);
            }
        },
        async readSerialTerminal() {
            console.log(this.espStub._parent);
            console.log(this.espStub._parent.__inputBuffer.join(' '));
        },
        logInfo(message) {
            this.logs.push(message);
        },
        logDebug(message) {
            this.logs.push(message);
        },
        logError(message) {
            this.logs.push(message);
        },
        async erase() {
            if (this.espStub !== undefined) {
                this.logInfo("Erasing flash memory. Please wait...");
                let stamp = Date.now();
                await this.espStub.eraseFlash();
                this.logInfo("Finished. Took " + (Date.now() - stamp) + "ms to erase.");
            }
        },
        async downloadFirmware() {
            this.download.started = true;

            try {
                const versionRes = await ApiCall.makeCall("GET", "1/public/firmware/version");
                this.download.state = 'Fetched Firmware version ' + versionRes.data.data.version;
                const response = await fetch(versionRes.data.data.downloadUri);
                this.download.state = 'Downloading Firmware version ' + versionRes.data.data.version + '...';

                const totalDownloadBytes = response.headers.get("content-length");
                let bytesDownloaded = 0;
                const chunks = [];
                const reader = response.body.getReader();
                while (true) {
                    const { value, done } = await reader.read();
                    if (done) {
                        this.download.progress = 1;
                        break;
                    }
                    chunks.push(value);
                    bytesDownloaded += value.length;
                    if (totalDownloadBytes != undefined) {
                        this.download.progress = bytesDownloaded / totalDownloadBytes;
                    }
                }

                const blob = new Blob(chunks);
                const arrayBuffer = await blob.arrayBuffer();
                this.firmware = arrayBuffer;
                this.download.state = 'Finished downloading Firmware version ' + versionRes.data.data.version;
                this.download.success = true;
                this.download.finished = true;
            } catch (error) {
                toastr.error("Error downloading fiormware", error);
                this.download.state = 'Error downloading firmware';
                this.download.success = false;
                this.download.finished = true;
            }
        },
        async flashData() {
            this.logInfo("Flashing memory. Please wait...");
            this.flash.started = true;
            let stamp = Date.now();
            await this.espStub.flashData(
                this.firmware,
                (bytesWritten, totalBytes) => {
                    this.flash.progress = bytesWritten / totalBytes;
                },
                0,
                true
            );
            this.logInfo("Finished. Took " + (Date.now() - stamp) + "ms to flash.");
            this.flash.finished = true;
        },
        async serialConnect() {

            this.connect.connecting = true;
            this.logs = [];
            if (this.espStub !== undefined) {
                await this.espStub.disconnect();
                await this.espStub.port.close();
                this.espStub = undefined;
            }
            const esploader = await connect({
                log: (...args) => this.logInfo(...args),
                debug: (...args) => this.logDebug(...args),
                error: (...args) => this.logError(...args),
            })

            await esploader.initialize();

            this.logInfo("Connected to " + esploader.chipName);
            this.logInfo("MAC Address: " + this.formatMacAddr(esploader.macAddr()));

            this.espStub = await esploader.runStub();
            this.processStream(this.espStub._parent._reader);
            this.connected = true;
            this.stage = 'flash';
        },
        formatMacAddr(macAddr) {
            return macAddr.map((value) => value.toString(16).toUpperCase().padStart(2, "0")).join(":");
        },
        async hardReset() {
            await this.espStub.hardReset();
        }
    },
    computed: {
        logsFormatted() {
            return this.logs.join("\n");
        },
        supported() {
            return ('serial' in navigator)
        }
    }
}
</script>

<style scoped lang="scss">
.main {
    text-align: center;
}
</style>