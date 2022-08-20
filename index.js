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
					}
					var playerFromNumber;
					//var isSameBrowserReconnect = jsonData.playerNumber != null;

					//if(!isSameBrowserReconnect) {
					playerFromNumber =  room["latestConnectionNum"]++;
					room["playerSockets"].push({
						socketId: socket.id,
						playerFromNumber: playerFromNumber,
						phoneId: phoneId
					});
					var initJson = {
						"data": {"action":"websocketInitialConnect"}, 
						"clientId": playerFromNumber
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
					//} 
					//this doesnt work anymore
					/*else {
						playerNumber = jsonData.playerNumber;
						room["playerSockets"][playerNumber] = socket.id;
					}*/

				} else {
					var roomDoesntExist = {
						"action":"roomDoesntExist"
					};
					socket.emit("systemMessage", roomDoesntExist);
					//socket.disconnect();
				}
			} else {
				//incase the user disconnected
				try {
					socket.join(roomCode);
					socket.rooms.forEach(rm => {
						var actualRoom = io.sockets.adapter.rooms.get(rm);
						if(actualRoom["computerSocket"]) {
							var socketInfos = actualRoom["playerSockets"];
							var playerFromNumber;
							if(socketInfos) {
								socketInfos.forEach(socketInfo => {
									if(socketInfo.phoneId == phoneId) {
										playerFromNumber = socketInfo.playerFromNumber;
									}
								})
							}
							var wrappedJsonData = {
								"data":jsonData, 
								"clientId": playerFromNumber
							};
							socket.to(actualRoom["computerSocket"].id).emit("phoneMessage", wrappedJsonData);
						}
					})
				} catch (error) {
					var criticalError = {
						"action":"criticalError"
					};
					socket.emit("systemMessage", criticalError);
				}
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
				var roomToJoin;
				if(!jsonData.roomCode) {
					var newRoomCode = getRandomString(4);
					while(io.sockets.adapter.rooms.get(newRoomCode)) {
						newRoomCode = getRandomString(4);
					}
					roomToJoin = newRoomCode;
				} else {
					roomToJoin = jsonData.roomCode
				}
				socket.join(roomToJoin);
				io.sockets.adapter.rooms.get(roomToJoin)["computerSocket"] = socket;
				io.sockets.adapter.rooms.get(roomToJoin)["latestConnectionNum"] = 0;
				
				if(!jsonData.roomCode) { 
					var roomCodeJson = {
						"roomCode":roomToJoin 
					};
					socket.emit("setRoomCode", roomCodeJson);
					logProductMetric("gameStarted", roomToJoin)
				}
				return;
			}
			
			if(jsonData.action === 'broadcast') {
				socket.rooms.forEach(room => {
					socket.to(room).emit("computerMessage", jsonData.data);
				});	
			} else if (jsonData.action === 'message') {
				socket.rooms.forEach(room => {
					var socketInfos = io.sockets.adapter.rooms.get(room)["playerSockets"];
					if(socketInfos) {
						socketInfos.forEach(socketInfo => {
							if(socketInfo.playerFromNumber == jsonData.from) {
								socket.to(socketInfo.socketId).emit("computerMessage", jsonData.data);	
							}
						})
					}
				})
			}
		});

		socket.on("disconnect", (reason) => {
			console.log("disconnect reason");
			console.log(reason);

			socket.adapter.rooms.forEach( room => {
				if(room.playerSockets) {
					room.playerSockets.forEach(socketInfo => {
						if(socketInfo.socketId == socket.id) {
							console.log("my socket: " + socketInfo.socketId + " playerNum: " + socketInfo.playerFromNumber);
							var notifyDisconnectJson = {
								"data": {"action":"notifyDisconnectJson"}, 
								"clientId": socketInfo.playerFromNumber
							};
							var computerSocketId = room["computerSocket"].id;
							socket.to(computerSocketId).emit("phoneMessage", notifyDisconnectJson);
						}
					});
				}
				//io.sockets.adapter.rooms.get(roomCode);
			});
		})
	});

	
	io.on("reconnect", () => {
		//does trigger idk why
	});

	//i think this does nothing
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