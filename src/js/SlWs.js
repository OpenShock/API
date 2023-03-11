global.onlineStates = {};

function newConnection() {
    const socket = new WebSocket(config.wsUrl);

    // Connection opened
    socket.onopen = (event) => {
        console.log("Connected to websocket")
    };
    
    // Listen for messages
    socket.onmessage = (event) => {
        console.log('Message from server ', event.data);

        const json = JSON.parse(event.data);
        switch(json.ResponseType) {
            case 10:
                json.Data.forEach(it => {
                    console.log(it);
                    onlineStates[it.Device] = it.Online;
                });
                break;
        }
    };
    
    socket.onclose = function(e) {
        console.log('Socket is closed. Reconnect will be attempted in 1 second.', e.reason);
        setTimeout(function() {
            newConnection();
        }, 1000);
    };

    global.ws = socket;
}

newConnection();