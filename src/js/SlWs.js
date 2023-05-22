import storeF from '@/store'
import * as signalR from '@microsoft/signalr'

const connection = new signalR.HubConnectionBuilder()
    .withUrl(config.apiUrl + "1/hubs/user")
    .configureLogging(signalR.LogLevel.Information)
    .withAutomaticReconnect()
    .build();

connection.on("DeviceStatus", (states) => {
    console.log(states);
    states.forEach(state => {
        storeF.dispatch('setDeviceState', {
            id: state.device,
            online: state.online
        })
    });
});

const ws = {
    async control(controlArr) {
        const res = await connection.invoke("Control", controlArr);
        console.log(res);
    },
    async captive(deviceId, enabled) {
        await connection.invoke("CaptivePortal", deviceId, enabled);
    }
}

connection.start().catch((err) => toastr.error(err, "Server connection"));

global.ws = ws;