//const crypto = require('crypto');
const express = require('express');
const { createServer } = require('http');
const WebSocket = require('ws');
const path = require('path');


const app = express();
const PORT = process.env.PORT || 3000;

app.get('/', (req, res) => {
  res.sendFile(path.join(__dirname, '/Assets/WebGLTemplates/AirConsole-2020/controller.html'));
})

app.use(express.static(path.join(__dirname, '/Assets/WebGLTemplates/AirConsole-2020')))
	.listen(PORT, () => console.log("Listening on ${PORT}"));
const server = createServer(app);
const wss = new WebSocket.Server({ server });
var connectionNumber = 0;
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

server.listen(8080, function() {
  var portNumber = process.env.PORT ? "" + process.env.PORT : "8080";
  console.log('Listening on http://localhost:' + portNumber);
});

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
