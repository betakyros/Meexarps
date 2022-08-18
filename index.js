//const crypto = require('crypto');
const { Console } = require('console');
const e = require('express');
const express = require('express');
const { json } = require('express/lib/response');
const { createServer } = require('http');
//const WebSocket = require('ws');

const path = require('path');


const app = express();
const PORT = process.env.PORT || 3000;

app.get('/', (req, res) => {
  res.sendFile(path.join(__dirname, '/Assets/WebGLTemplates/AirConsole-2020/keyboard.html'));
});
app.use(express.static(path.join(__dirname, '/Assets/WebGLTemplates/AirConsole-2020')));

const server = app.listen(PORT, () => console.log(`Listening on ${PORT}`));
const io = require("socket.io")(server, {
	pingTimeout: 600000,
	cors: {
		origin: "http://localhost:3000"
	}
});

  	io.on("connection", (socket) => {
		  //for debug logging
	  socket.onAny((eventName, ...args) => {
		  var isComputer =  eventName.includes("computer");
		  var color = isComputer ? "\x1b[33m" : eventName.includes("phone") ? "\x1b[36m" : "";

		  if(Array.isArray(args)) {
			console.log(color, eventName, args);
			console.log(isComputer ? 'compMessage: ' : 'phoneMessage', args);
		  } else {
			console.log(color,eventName, args);
			console.log(isComputer ? 'compMessage: ' : 'phoneMessage', args);
		  }
		});

		socket.on("phoneMessage", (data) => {
			const phoneId = socket.handshake.query.phoneId;
			let jsonData = JSON.parse(data);
			let roomCode = "";
			if(jsonData.roomCode) {
				roomCode = jsonData.roomCode.toUpperCase().trim();
			} else {
				//ignore this phone message. it is malformed
			}
			if(jsonData.action === "metric") {
				logProductMetric(jsonData.metricName, roomCode);
			
			} else if(jsonData.action == "system") {	
				if(io.sockets.adapter.rooms.get(roomCode)) {
					socket.join(roomCode);
					logProductMetric("PlayerJoined", roomCode)
					var room = io.sockets.adapter.rooms.get(roomCode);
					if(!room["playerSockets"]) {
						room["playerSockets"] = [];
						room["phoneIds"] = [];
					}
					var playerNumber;
					var isSameBrowserReconnect = jsonData.playerNumber != null;

					if(!isSameBrowserReconnect) {
						room["playerSockets"].push(socket.id)
						playerNumber =  room["latestConnectionNum"]++;
						room["phoneIds"][playerNumber] = phoneId;
						var initJson = {
							"data": {"action":"websocketInitialConnect"}, 
							"clientId": playerNumber
						};
						if(!room["computerSocket"]) {
							console.log("ERROR: computer connection does not exist.");
							console.log("Room: ");
							console.log(room);
							console.log("Computer Socket: ");
							console.log(room["computerSocket"]);
						}
						var computerSocketId = room["computerSocket"].id;
						socket.to(computerSocketId).emit("phoneMessage", initJson);
					} else {
						playerNumber = jsonData.playerNumber;
						room["playerSockets"][playerNumber] = socket.id;
					}

				} else {
					var roomDoesntExist = {
						"action":"roomDoesntExist"
					};
					socket.emit("systemMessage", roomDoesntExist);
					//socket.disconnect();
				}
			} else {
				//incase the user disconnected
				socket.join(roomCode);
				socket.rooms.forEach(rm => {
					if(io.sockets.adapter.rooms.get(rm)["computerSocket"]) {
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

			if(jsonData.action === "log") {
				console.log(jsonData.message);
				if(jsonData.context === "error") {
					logProductMetric("error");
				} else if(jsonData.context === "sendEndScreen") {
					logProductMetric("sendEndScreen");
				}
				return;
			}

			if(jsonData.action === "metric") {
				logProductMetric(jsonData.metricName, jsonData.roomCode);
				return;
			}

			if(jsonData.action === "init") {
				var newRoomCode = getRandomString(4);
				while(io.sockets.adapter.rooms.get(newRoomCode)) {
					newRoomCode = getRandomString(4);
				}
				socket.join(newRoomCode);
				io.sockets.adapter.rooms.get(newRoomCode)["computerSocket"] = socket;
				io.sockets.adapter.rooms.get(newRoomCode)["latestConnectionNum"] = 0;
				var roomCodeJson = {
					"roomCode":newRoomCode 
				};
				socket.emit("setRoomCode", roomCodeJson);
				logProductMetric("gameStarted", newRoomCode)
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
			}
		});
	});

	
	io.on("reconnect", () => {
		//does trigger idk why
	});

  	io.on("disconnect", () => {
		console.log("disconnectServer"); // "ping timeout"
		logProductMetric("disconnectServer")
	  });

function getRandomString(length) {
    var randomChars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
    var result = '';
    for ( var i = 0; i < length; i++ ) {
        result += randomChars.charAt(Math.floor(Math.random() * randomChars.length));
    }
    return result;
}

function logProductMetric(metric, context) {
	var contextPart = "";
	if(context) {
		contextPart = "." + context;
	}
	console.log("MEEXARPSMETRICS." + metric + contextPart)
}