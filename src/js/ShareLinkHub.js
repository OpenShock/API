import * as signalR from '@microsoft/signalr'

export default class ws {
    async control(id, intensity, duration, type) {
        const ctrl = [
            {
                Id: id,
                Type: type,
                Duration: duration,
                Intensity: intensity
            },
        ];

        await this.controlMultiple(ctrl);
    }

    async controlMultiple(shocks) {
        const res = await this.connection.invoke("Control", shocks);
    }

    constructor(id) {

        this.connection = new signalR.HubConnectionBuilder()
        .withUrl(`${config.apiUrl}1/hubs/share/link/${id}?name=AAAAAAAAAAAA`)
        .configureLogging(signalR.LogLevel.Information)
        .withAutomaticReconnect([0, 1000, 2000, 5000, 10000, 10000, 15000, 30000, 60000])
        .build();

        this.connection.on("Updated", () => {
        
        });
    }

    start() {
        this.connection.start().catch((err) => toastr.error(err, "Share Link Hub"));
    }

    stop() {
        this.connection.stop();
    }
}