import storeF from '@/store'

function newConnection() {
    const socket = new WebSocket(config.wsUrl);

    // Connection opened
    socket.onopen = (event) => {
        console.log("Connected to websocket")
    };
    
    // Listen for messages
    socket.onmessage = (event) => {
        const json = JSON.parse(event.data);
        switch(json.ResponseType) {
            case 10:
                json.Data.forEach(it => {
                    storeF.dispatch('setDeviceState', {
                        id: it.Device,
                        online: it.Online
                    })
                });
                break;
        }
    };
    
    socket.onclose = function(e) {
        console.log('Socket is closed. Reconnect will be attempted in 5 seconds.', e.reason);
        setTimeout(function() {
            newConnection();
        }, 5000);
    };

    global.ws = socket;
}

newConnection();