import storeF from '@/store'
import * as signalR from '@microsoft/signalr'

const connection = new signalR.HubConnectionBuilder()
    .withUrl(config.apiUrl + "1/hubs/user")
    .configureLogging(signalR.LogLevel.Information)
    .withAutomaticReconnect([0, 1000, 2000, 5000, 10000, 10000, 15000, 30000, 60000])
    .build();

connection.on("DeviceStatus", (states) => {
    states.forEach(state => {
        storeF.dispatch('setDeviceState', {
            id: state.device,
            online: state.online
        })
    });
});

const ws = {
    async control(id, intensity, duration, type, customName = null) {
        const ctrl = [
            {
                Id: id,
                Type: type,
                Duration: parseFloat(duration) * 1000,
                Intensity: parseInt(intensity)
            },
        ];

        await this.controlMultiple(ctrl, customName);
    },
    async controlMultiple(shocks, customName) {
        const res = await connection.invoke("ControlV2", shocks, customName);
        console.log(res);
    },
    async captive(deviceId, enabled) {
        await connection.invoke("CaptivePortal", deviceId, enabled);
    }
}

setInterval(() => {
    if(storeF.state.userHubState != connection._connectionState) {
        storeF.commit('setUserHubState', connection._connectionState);
        console.log(connection._connectionState);
    }
}, 200);

connection.start().catch((err) => toastr.error(err, "Server connection"));

global.ws = ws;
global.userHubConnection = connection;