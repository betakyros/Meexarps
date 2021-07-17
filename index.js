//const crypto = require('crypto');
const express = require('express');
const { createServer } = require('http');
//const WebSocket = require('ws');

const path = require('path');


const app = express();
const PORT = process.env.PORT || 3000;

app.get('/', (req, res) => {
  res.sendFile(path.join(__dirname, '/Assets/WebGLTemplates/AirConsole-2020/keyboard.html'));
});

app.use(express.static(path.join(__dirname, '/Assets/WebGLTemplates/AirConsole-2020')));
const server = createServer(app);
const io = require("socket.io")(server, {});

var computerSocket;
var playerSockets = [];
  io.on("connection", (socket) => {
	  console.log("connection started");

	  socket.join("room1");
	  console.log(socket.rooms); 
	  socket.onAny((eventName, ...args) => {
		console.log(eventName, args);
		});

		socket.on("phoneMessage", (data) => {
			var jsonData = JSON.parse(data);
			console.log("phoneMessage", jsonData);
			console.log("data", jsonData.action);
			if(jsonData.action == "system") {
				console.log("phoneMessage - system", jsonData);	
				var playerNumber = playerSockets.push(socket.id) - 1;
				var initJson = {
					"data": {"action":"websocketInitialConnect"}, 
					"clientId": playerNumber
				};
				console.log("computerSocket: " + computerSocket);
				console.log("roomCode: " + jsonData.roomCode);
				//socket.to(computerSocket.id).emit("phoneMessage", initJson);
			}
			if(computerSocket) {
				console.log("phoneMessage - normal", jsonData);	
				var playerNumber = playerSockets.lastIndexOf(socket.id)
				var wrappedJsonData = {
					"data":jsonData, 
					"clientId": playerNumber
				};
				socket.to(computerSocket.id).emit("phoneMessage", wrappedJsonData);
			}
		});
	
		socket.on("computerMessage", (data) => {
			if(data == "init") {
				console.log("setting computer socket: " + socket);

				computerSocket = socket;
				return;
			}
			
			var jsonData = data;
			console.log("computer message: " + jsonData);
			console.log("computerSocket: " + computerSocket);
			console.log("!computerSocket: " + !computerSocket);

			if(data.action == 'broadcast') {
				console.log("broadcasting", data.data);
				socket.broadcast.emit("computerMessage", jsonData.data);	
			} else if (data.action == 'message') {
				console.log("messaging " + jsonData.from);
				socket.to(playerSockets[jsonData.from]).emit("computerMessage", jsonData.data);
			} else {
				console.log("unknown computer message");
			}
		});
	});
  io.on("disconnect", (reason) => {
		console.log(reason); // "ping timeout"
	  });
  io.on("open", (data) => {
	  console.log("open", data);
	});
  
	io.on("message", (data) => {
		console.log("message", data);
	});

	

server.listen(PORT, () => console.log("Listening on ${PORT}"));
/*
io.use((socket, next) => {
	console.log("used");
});
*/
//const wss = new WebSocket.Server({ server });
var connectionNumber = 0;/*
wss.on('connection', function(ws) {
  console.log("client joined.");
  ws.id = connectionNumber;
  connectionNumber ++;
  console.log("client id: " + ws.id);

  ws.on('message', function(input) {
	console.log("-----------------");
	var data = JSON.parse(input);
    console.log("clientId: " + ws.id + " data: " + JSON.stringify(data));
	
	if(data.action === "broadcast") {
		wss.clients.forEach(function each(client) {
			if(client.isPhone) {
				console.log("Broadcasting to clientId: " + client.id + " data: " + JSON.stringify(data.data));
				client.send(JSON.stringify(data.data));
			}
		  }
		)		
	} else if(data.action === "message") {
		var clientId = data.from;
		console.log("Messaging clientId: " + clientId);
		getStreamByClientId(clientId).send(JSON.stringify(data.data));
	} else if(data.action === "system") {
		console.log("setting system info. isPhone: " + data.isPhone + " playerNumber " + data.playerNumber);

		ws.isPhone = data.isPhone;
		ws.playerNumber = data.playerNumber;
		
		if(ws.isPhone) {
			var initJson = '{"data":{"action":"websocketInitialConnect"}, "clientId":' + ws.id + ' }';
			getTvScreen().send(initJson);
		}
	} else if(data.action === "phoneMessage") {
		var message = data.message;
		var wrappedMessage = {
			'clientId' : ws.id,
			'data' : message
		};
		console.log("phoneMessage: " + JSON.stringify(message));
		wss.clients.forEach(function each(client) {
			if(!client.isPhone) {
				console.log("phoneMessage to clientId:" + client.id);
				client.send(JSON.stringify(wrappedMessage));
			}
		})
	} else {
		console.log("unknown action");
	}
	console.log("-----------------");

  });

  ws.on('close', function() {
    console.log("client left.");
  });
});
*/

function getTvScreen() {
	for (var it = wss.clients.values(), currWs= null; currWs=it.next().value; ) {
		//-1 is unity
		if(currWs.playerNumber == -1) {
			return currWs;
		}
	}
	console.log("getTvScreen could not find the target!");
}

function getStreamByClientId(targetClientId) {
	for (var it = wss.clients.values(), currWs= null; currWs=it.next().value; ) {
		if(currWs.id == targetClientId) {
			return currWs;
		}
	}
}
