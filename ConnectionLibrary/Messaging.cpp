#include "Messaging.h"
#include "Classes.h"
#include "Sockets.h"
#define DEBUG

// Message ---------------------------------------------------------------------
int Message::lastID = 0;

Message::Message(IMessageType* object, const Host& From, const Host& To,
				 bool confirmDelivery, bool confirmProcessing, bool store)
{
	id = ++lastID;
	if (object != NULL)
	{
		body = object->ToByteArray();
		type = object->GetType();
	}
    this->From = From;
	this->To = To;
	protocolVersion = PROTOCOL_VERSION;
	RequireDeliveryConfirm = confirmDelivery;
	RequireProcessingConfirm = confirmProcessing;
	StoreInBox = store;
	delivered = false;
	processed = false;
}

// Connection ------------------------------------------------------------------
Messaging::Messaging(int logLevel)
{
	ObjectFactory::RegisterKnownTypes();
	LogLevel = qMin(9, qMax(0, logLevel));
	tcpServer = new QTcpServer();
	connect(tcpServer, SIGNAL(newConnection()), this, SLOT(incConnected()));
}

Message Messaging::ComposeMessage(IMessageType* object, const Host& To, bool confirmDelivery,
								   bool confirmProcessing, bool storeInBox) const
{
	return Message(object, Local, To, confirmDelivery,
				   confirmProcessing, storeInBox);
}

bool Messaging::SendMessage(Message message)
{
	outgoingMutex.lock();
	outgoing[message.To].append(message);
	outgoingMutex.unlock();
    outSocketsMutex.lock();
	if (outSockets.count(message.To) == 0)
	{
		outSockets[message.To] = new WriterSocket(this, message.To);
		connect(outSockets[message.To], SIGNAL(disconnected()), this, SLOT(disconnected()));
	}
    outSocketsMutex.unlock();
	outSockets[message.To]->Connect();
	return true;
}

bool Messaging::Log(const QString& logMessage, int logLevel) const
{
	if (qMin(9, qMax(1, logLevel)) <= LogLevel) emit NewLogMessage(logMessage);
	return qMin(9, qMax(1, logLevel)) <= LogLevel;
}

bool Messaging::Listen(const QString& IP, unsigned short port)	//<-Server
{
	Local = Host(IP, port);
	if (!tcpServer->listen(QHostAddress(IP), port))
	{
		Log(QString("<font color=red>Failed to bind to port</font>"), 1);
		return false;
	}
	Log(QString("<font color=green>Listening on %1:%2</font>").arg(
		tcpServer->serverAddress().toString()).arg(tcpServer->serverPort()), 7);
	return true;
}

void Messaging::Connect(const QString& IP, unsigned short Port)	//<-Client
{
	Host h(IP, Port);
	if (outSockets.count(h) == 0)
	{
		outSockets[h] = new WriterSocket(this, h);
	}
	outSockets[h]->Connect();
	Log(QString("Connecting to %1:%2...").arg(IP).arg(Port), 7);
}

void Messaging::Disconnect(const QString& IP, unsigned short Port)
{
	outSockets[Host(IP, Port)]->Disconnect();
}

void Messaging::incConnected()		//<-Server
{
	ReaderSocket* socket = new ReaderSocket(this, tcpServer->nextPendingConnection());
	connect(socket, SIGNAL(disconnected()), this, SLOT(disconnected()));
	Log(QString("<font color=orange>%1 connected.</font>").arg(socket->Peer().ToString()), 2);
}

void Messaging::disconnected()		//<-Client
{
	Log(QString("<font color=orange>Disconnected from remote host.</font>"), 2);
}

const QMap<int, Message>& Messaging::Inbox() const
{
	return inbox;
}

void Messaging::ClearInbox()
{
	inbox.clear();
}

const QMap<int, Message>& Messaging::Outbox() const
{
	return outbox;
}

void Messaging::ClearOutbox()
{
	outbox.clear();
}

void Messaging::tuneSocketParams(int packetSize, double packetLossProbability, double byteChangeProbability)
{
#ifdef DEBUG
	WriterSocket* socket;
	foreach (socket, outSockets)
	{
		socket->tuneSocketParams(packetSize, packetLossProbability, byteChangeProbability);
	}
#endif
}
