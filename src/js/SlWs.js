import storeF from '@/store'
import { faConfluence } from '@fortawesome/free-brands-svg-icons';
import * as signalR from '@microsoft/signalr'

const connection = new signalR.HubConnectionBuilder()
    .withUrl(config.apiUrl + "1/hubs/user")
    .configureLogging(signalR.LogLevel.Information)
    .withAutomaticReconnect([0, 1000, 2000, 5000, 10000, 15000, 30000, 60000, 60000, 60000, 60000, 60000, 60000, 60000, 60000, 60000, 60000, 60000])
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
    async control(controlArr) {
        const res = await connection.invoke("Control", controlArr);
        console.log(res);
    },
    async captive(deviceId, enabled) {
        await connection.invoke("CaptivePortal", deviceId, enabled);
    }
}

setInterval(() => {
    if(storeF.state.userHubState != connection._connectionState) {
        storeF.commit('setUserHubState', connection._connectionState);
    }
}, 250);

connection.start().catch((err) => toastr.error(err, "Server connection"));

global.ws = ws;
global.userHubConnection = connection;