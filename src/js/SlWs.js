const socket = new WebSocket(config.wsUrl);

// Connection opened
socket.addEventListener('open', (event) => {
    console.log("Connected to websocket")
});

// Listen for messages
socket.addEventListener('message', (event) => {
    console.log('Message from server ', event.data);
});

global.ws = socket;