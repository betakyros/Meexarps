//const crypto = require('crypto');
const { Console } = require('console');
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

  io.on("connection", (socket) => {
	  console.log("connection started");
	  socket.onAny((eventName, ...args) => {
		  var color = eventName.includes("computer") ? "\x1b[33m" : eventName.includes("phone") ? "\x1b[36m" : "";

		  if(Array.isArray(args)) {
			console.log(color, eventName, args);
			console.log('compMessage: ', args);
		  } else {
			console.log(color,eventName, args);
			console.log('compMessage: ', args);
		  }
		});

		socket.on("phoneMessage", (data) => {
			console.log("queryParams.phoneId");
			const phoneId = socket.handshake.query.phoneId;
			console.log(socket.handshake.query.phoneId);
			let jsonData = JSON.parse(data);
			let roomCode = jsonData.roomCode.toUpperCase().trim();
			if(jsonData.action == "system") {	
				console.log("system");
				
				if(io.sockets.adapter.rooms.get(roomCode)) {
					socket.join(roomCode);
					var room = io.sockets.adapter.rooms.get(roomCode);
					if(!room["playerSockets"]) {
						room["playerSockets"] = [];
						room["phoneIds"] = [];
					}
					var playerNumber;
					var isSameBrowserReconnect = jsonData.playerNumber != null;
					if(!isSameBrowserReconnect) {
						
						playerNumber = room["playerSockets"].push(socket.id) - 1;
						room["phoneIds"][playerNumber] = phoneId;
						var initJson = {
							"data": {"action":"websocketInitialConnect"}, 
							"clientId": playerNumber
						};
						var computerSockeId = io.sockets.adapter.rooms.get(roomCode)["computerSocket"].id;
						socket.to(computerSockeId).emit("phoneMessage", initJson);
					} else {
						console.log("sameBrowserReconnect");
						playerNumber = jsonData.playerNumber;
						io.sockets.adapter.rooms.get(roomCode)["playerSockets"][playerNumber] = socket.id;
					}

				} else {
					console.log("roomDoesntExist");	
					console.log("rooms: " + JSON.stringify(io.sockets.adapter.rooms));		

					var roomDoesntExist = {
						"action":"roomDoesntExist"
					};
					socket.emit("systemMessage", roomDoesntExist);
					socket.disconnect();
				}
			} else {
				console.log("not system");
				//incase the user disconnected
				socket.join(roomCode);
				socket.rooms.forEach(rm => {
					console.log("Curr Room: " + rm + " myRoom: " + roomCode);
					if(io.sockets.adapter.rooms.get(rm)["computerSocket"]) {
						console.log("phoneMessage - normal", JSON.stringify(jsonData));	
						var playerNumber = io.sockets.adapter.rooms.get(rm)["phoneIds"].lastIndexOf(phoneId)
						var wrappedJsonData = {
							"data":jsonData, 
							"clientId": playerNumber
						};
						socket.to(io.sockets.adapter.rooms.get(rm)["computerSocket"].id).emit("phoneMessage", wrappedJsonData);
					}
				})
			}
		});
	
		socket.on("computerMessage", (data) => {
			// when running in unity, data comes in as a json object. 
			// when running in browser, data comes in as a string
			var jsonData = data;
			if(typeof data === "string") {
				jsonData = JSON.parse(data);
			} 
			if(jsonData.action === "init") {
				var newRoomCode = getRandomString(4);
				while(io.sockets.adapter.rooms.get(newRoomCode)) {
					newRoomCode = getRandomString(4);
				}
				socket.join(newRoomCode);
				io.sockets.adapter.rooms.get(newRoomCode)["computerSocket"] = socket;
				var roomCodeJson = {
					"roomCode":newRoomCode 
				};
				socket.emit("setRoomCode", roomCodeJson);
				console.log("roomCode: " + roomCodeJson);
				return;
			}
			
			if(jsonData.action === 'broadcast') {
				socket.rooms.forEach(room => {
					socket.to(room).emit("computerMessage", jsonData.data);
				});	
			} else if (jsonData.action === 'message') {
				socket.rooms.forEach(room => {
					if(io.sockets.adapter.rooms.get(room)["playerSockets"]) {
						var playerSocket = io.sockets.adapter.rooms.get(room)["playerSockets"][jsonData.from];
						socket.to(playerSocket).emit("computerMessage", jsonData.data);	
					}
				})
			} else {
				console.log("unknown computer message");
			}
		});
	});
  io.on("disconnect", (reason) => {
		console.log("disconnect", reason); // "ping timeout"
	  });
  io.on("open", (data) => {
	  console.log("open", data);
	});
  
	io.on("message", (data) => {
		console.log("message", data);
	});

	

server.listen(PORT, () => console.log(`Listening on ${PORT}`));
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

function getRandomString(length) {
    var randomChars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
    var result = '';
    for ( var i = 0; i < length; i++ ) {
        result += randomChars.charAt(Math.floor(Math.random() * randomChars.length));
    }
    return result;
}
