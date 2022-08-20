mergeInto(LibraryManager.library, {
	InitializeSIOVars: function () {
		window.UnitySocketIOInstances = [];
		window.UnitySocketIOInstanceGameObjects = [];
		window.UnitySocketIOAuthPayloads = [];
	},
	
	CreateSIOInstance: function (instanceName, gameObjectName, targetAddress, enableReconnect, payload) {
		var iName = UTF8ToString(instanceName);
		var goName = UTF8ToString(gameObjectName);
		var connectPayload = UTF8ToString(payload);
		
		window.UnitySocketIOInstanceGameObjects[iName] = goName;
		
		var fullAddress = UTF8ToString(targetAddress);
		var serverSepIndex = fullAddress.indexOf('/', 8);
		var serverAddress = (serverSepIndex > 0 ? fullAddress.substr(0, serverSepIndex) : fullAddress);
		var serverPath = (serverSepIndex > 0 ? fullAddress.substr(serverSepIndex) : '/socket.io/');
		
		var queryParams = {};
		window.UnitySocketIOAuthPayloads[iName] = null;
		if (fullAddress.indexOf('?') !== false) {
			var queryString = fullAddress.substring(fullAddress.indexOf('?') + 1).split('&');
			for(var i = 0; i < queryString.length; i++){
				queryString[i] = queryString[i].split('=');
				queryParams[queryString[i][0]] = decodeURIComponent(queryString[i][1]);
			}
		}
		
		if (connectPayload != null && connectPayload.length > 1) {
			var connectPayloadObject = null;
			try {
				connectPayloadObject = JSON.parse(connectPayload);
				window.UnitySocketIOAuthPayloads[iName] = connectPayloadObject;
			} catch (err) {
				console.error('Error parsing Socket.IO authPayload on ' + iName + ': ' + err);
				window.UnitySocketIOAuthPayloads[iName] = null;
			}
		}
		
		console.log('Connecting SIO to ' + serverAddress + ' on path ' + serverPath + ' with autoReconnect ' + (enableReconnect == 1 ? 'enabled' : 'disabled'));
		
		window.UnitySocketIOInstances[iName] = window.io.connect(serverAddress, {
			transports: ['websocket'],
			reconnection: (enableReconnect == 1),
			reconnectionDelay: 1000,
			reconnectionDelayMax: 8000,
			timeout: 5000,
			upgrade: true,
			rememberUpgrade: true,
			path: serverPath,
			query: queryParams,
			autoConnect: true,
			auth: function(cb) {
				cb(window.UnitySocketIOAuthPayloads[iName]);
			}
		});
		
		window.UnitySocketIOInstances[iName].on('connect', function() {
			SendMessage(window.UnitySocketIOInstanceGameObjects[iName], 'UpdateSIOSocketID', window.UnitySocketIOInstances[iName].id);
			SendMessage(window.UnitySocketIOInstanceGameObjects[iName], 'UpdateSIOStatus', 1); //connected
			SendMessage(window.UnitySocketIOInstanceGameObjects[iName], 'RaiseSIOEvent', JSON.stringify({
				eventName: 'connect',
				data: null
			}));
		});
		
		window.UnitySocketIOInstances[iName].on('disconnect', function(reason) {
			SendMessage(window.UnitySocketIOInstanceGameObjects[iName], 'UpdateSIOStatus', 0); //disconnected
			SendMessage(window.UnitySocketIOInstanceGameObjects[iName], 'RaiseSIOEvent', JSON.stringify({
				eventName: 'disconnect',
				data: reason
			}));
		});
		
		window.UnitySocketIOInstances[iName].on('reconnect', function(attemptNumber) {
			SendMessage(window.UnitySocketIOInstanceGameObjects[iName], 'UpdateSIOSocketID', window.UnitySocketIOInstances[iName].id);
			SendMessage(window.UnitySocketIOInstanceGameObjects[iName], 'UpdateSIOStatus', 1); //connected
			SendMessage(window.UnitySocketIOInstanceGameObjects[iName], 'RaiseSIOEvent', JSON.stringify({
				eventName: 'reconnect',
				data: attemptNumber
			}));
		});
		
		window.UnitySocketIOInstances[iName].on('connect_timeout', function() {
			SendMessage(window.UnitySocketIOInstanceGameObjects[iName], 'UpdateSIOStatus', 2); //errored
			SendMessage(window.UnitySocketIOInstanceGameObjects[iName], 'SIOWarningRelay', 'Timeout on connection ' + iName);
			SendMessage(window.UnitySocketIOInstanceGameObjects[iName], 'RaiseSIOEvent', JSON.stringify({
				eventName: 'connect_timeout',
				data: null
			}));
		});
		
		window.UnitySocketIOInstances[iName].on('connect_error', function(error) {
			SendMessage(window.UnitySocketIOInstanceGameObjects[iName], 'UpdateSIOStatus', 2); //errored
			SendMessage(window.UnitySocketIOInstanceGameObjects[iName], 'SIOWarningRelay', 'Error on connection attempt for ' + iName + ': ' + error);
			SendMessage(window.UnitySocketIOInstanceGameObjects[iName], 'RaiseSIOEvent', JSON.stringify({
				eventName: 'connect_error',
				data: error
			}));
		});
		
		window.UnitySocketIOInstances[iName].on('reconnect_attempt', function() {
			window.UnitySocketIOInstances[iName].io.opts.transports = ['polling', 'websocket'];
			SendMessage(window.UnitySocketIOInstanceGameObjects[iName], 'SIOWarningRelay', 'Websocket failed for ' + iName + '. Trying to reconnect with polling enabled.');
			SendMessage(window.UnitySocketIOInstanceGameObjects[iName], 'RaiseSIOEvent', JSON.stringify({
				eventName: 'reconnect_attempt',
				data: null
			}));
		});
		
		window.UnitySocketIOInstances[iName].on('reconnect_error', function(error) {
			SendMessage(window.UnitySocketIOInstanceGameObjects[iName], 'UpdateSIOStatus', 2); //errored
			SendMessage(window.UnitySocketIOInstanceGameObjects[iName], 'SIOWarningRelay', 'Error on reconnection attempt for ' + iName + ': ' + error);
			SendMessage(window.UnitySocketIOInstanceGameObjects[iName], 'RaiseSIOEvent', JSON.stringify({
				eventName: 'reconnect_error',
				data: error
			}));
		});
		
		window.UnitySocketIOInstances[iName].on('reconnect_failed', function(error) {
			SendMessage(window.UnitySocketIOInstanceGameObjects[iName], 'UpdateSIOStatus', 2); //errored
			SendMessage(window.UnitySocketIOInstanceGameObjects[iName], 'SIOWarningRelay', 'Reconnect failed for ' + iName + ': Max. attempts exceeded.');
			SendMessage(window.UnitySocketIOInstanceGameObjects[iName], 'RaiseSIOEvent', JSON.stringify({
				eventName: 'reconnect_failed',
				data: error
			}));
		});
		
		window.UnitySocketIOInstances[iName].onAny(function (eName, data) {
			SendMessage(window.UnitySocketIOInstanceGameObjects[iName], 'RaiseSIOEvent', JSON.stringify({
				eventName: eName,
				data: (typeof data == 'undefined' ? null : (typeof data == 'string' ? data : JSON.stringify(data)))
			}));
		});
	},
	
	CloseSIOInstance: function (instanceName) {
		var iName = UTF8ToString(instanceName);
		try {
			if (typeof window.UnitySocketIOInstances[iName] !== 'undefined' && window.UnitySocketIOInstances[iName] != null) {
				window.UnitySocketIOInstances[iName].close();
			}
		} catch(e) {
			console.warn('Exception while closing SocketIO connection on ' + iName + ': ' + e);
		}
	},
	
	DestroySIOInstance: function (instanceName) {
		var iName = UTF8ToString(instanceName);
		console.log('Destroying SIO instance ' + iName);
		if (typeof window.UnitySocketIOInstances[iName] !== 'undefined' && window.UnitySocketIOInstances[iName] != null) {
			window.UnitySocketIOInstances[iName].removeAllListeners();
		}
		delete window.UnitySocketIOInstances[iName];
		delete window.UnitySocketIOInstanceGameObjects[iName];
	},
		
	SIOEmitNoData: function (instanceName, eventName) {
		var iName = UTF8ToString(instanceName);
		if (typeof window.UnitySocketIOInstances[iName] !== 'undefined') {
			window.UnitySocketIOInstances[iName].emit(UTF8ToString(eventName));
		} else {
			console.warn('The scripts on ' + iName + ' tried to emit data to an eighter closed or never connected Socket.IO instance. This should not happen.');
		}
	},
	
	SIOEmitWithData: function (instanceName, eventName, data, parseAsJSON) {
		var iName = UTF8ToString(instanceName);
		var parsedData = "__ERROR__";
		if (typeof window.UnitySocketIOInstances[iName] !== 'undefined') {
			if (parseAsJSON == 1) {
				parsedData = JSON.parse(UTF8ToString(data));
			}
			else 
			{
				parsedData = UTF8ToString(data)
			}
			window.UnitySocketIOInstances[iName].emit(UTF8ToString(eventName), parsedData);
		} else {
			console.warn('The scripts on ' + iName + ' tried to emit data to an eighter closed or never connected Socket.IO instance. This should not happen.');
		}
	}
});
