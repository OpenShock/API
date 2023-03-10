function newConnection() {
    const socket = new WebSocket(config.wsUrl);

    // Connection opened
    socket.onopen = (event) => {
        console.log("Connected to websocket")
    };
    
    // Listen for messages
    socket.onmessage = (event) => {
        console.log('Message from server ', event.data);
    };
    
    ws.onclose = function(e) {
        console.log('Socket is closed. Reconnect will be attempted in 1 second.', e.reason);
        setTimeout(function() {
            newConnection();
        }, 1000);
    };

    global.ws = socket;
}