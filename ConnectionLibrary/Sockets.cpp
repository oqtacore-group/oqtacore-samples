#include "Sockets.h"
#include <QVector>
#include "Messaging.h"
#include "Classes.h"
#include <QCryptographicHash>

// AbstractSocket ----------------------------------------------------------------
AbstractSocket::AbstractSocket()
{
	socket = new TCP_SOCKET(this);
	connect(socket, SIGNAL(error(QAbstractSocket::SocketError)),
			this, SLOT(displayError(QAbstractSocket::SocketError)));
	connect(socket, SIGNAL(disconnected()), this, SLOT(onDisconnect()));
	connect(socket, SIGNAL(disconnected()), this, SLOT(deleteLater()));
}

QAbstractSocket::SocketState AbstractSocket::State() const
{
	return socket->state();
}

Host AbstractSocket::Peer() const
{
	return Host(socket->peerAddress().toString(), socket->peerPort());
}

Host AbstractSocket::Local() const
{
	return Host(socket->localAddress().toString(), socket->localPort());
}

void AbstractSocket::displayError(QAbstractSocket::SocketError socketError)
{
	if (parent() == NULL) return;
	((Messaging*)parent())->Log(QString("<font color=red>Error(%1): %2</font>").
			arg(socketError).arg(socket->errorString()), 1);
}

void AbstractSocket::onDisconnect()
{
	emit disconnected();
}

void AbstractSocket::tuneSocketParams(int packetSize, double packetLossProbability, double byteChangeProbability)
{
}

QString AbstractSocket::BitsToString(QByteRef Byte)
{
	QString temp = "";
	for(int i = 0; i < 8; i++)
		if (((char)Byte) & (1<<i))
			temp += "1";
		else
			temp += "0";
	return temp;
}

void AbstractSocket::outputByteArray(QByteArray byteArray)
{
	for (int i = 0; i < byteArray.count(); i++)
	{
		if (BitsToString(byteArray[i]) == "00000000"){}
			//qDebug() << i << ") \"00000000\" = 0 = NULL";
		else
		{
			//qDebug() << i << ")" << BitsToString(byteArray[i]) << "=" << (int)byteArray[i] << "=" << (char)byteArray[i];
		}
	}
}

// ReaderSocket ----------------------------------------------------------------
ReaderSocket::ReaderSocket(Messaging *parent, QTcpSocket* sock): AbstractSocket()
{
	setParent(parent);
	if (sock != 0)
	{
		delete socket;
#ifndef DEBUG
		socket = sock;
#endif
	}
	nextBlockSize = 0;
	connect(socket, SIGNAL(readyRead()), this, SLOT(read()));
	socket->setReadBufferSize(0);
	nextBlockSize = 0;
}

void ReaderSocket::read()
{
	((Messaging*)parent())->Log("Attempt to read smth", 9);
#ifdef DEBUG
	QDataStream& in = socket->data;
#else
	QDataStream in(socket);
#endif
	static Message message; 
	in.setVersion(QDataStream::Qt_4_3);
	static QByteArray md5;
    forever			
    {
		if (nextBlockSize == 0)
		{
			message = Message(NULL, Peer(), Local());
			if (socket->bytesAvailable() < 33*sizeof(char))
				break;
			in >> protocol >> flags >> type >> replyId >> port >> md5 >> nextBlockSize;
			message.From.Port = port;
			message.type = type;
			message.RequireDeliveryConfirm = flags && 128;
			message.RequireProcessingConfirm = flags && 64;
			((Messaging*)parent())->Log(QString("Message of type %1").arg(type), 8);;
		}
		if (socket->bytesAvailable() < nextBlockSize)
		{
			((Messaging*)parent())->Log(QString("Not enough data (%1 < %2) - waiting...")
					.arg(socket->bytesAvailable()).arg(nextBlockSize), 10);
			break;
		}
		else
		{
			((Messaging*)parent())->Log(QString("Enough data (socket->bytesAvailable()=%1 < nextBlockSize=%2)...")
				.arg(socket->bytesAvailable()).arg(nextBlockSize), 10);
		}
		socket->read(4);
		message.body = socket->read(nextBlockSize);
		nextBlockSize = 0;
		if (md5 != QCryptographicHash::hash(message.body, QCryptographicHash::Md5))
		{
			emit ((Messaging*)parent())->Error("Hash corrupted");
			return;
		}
        if (message.type == 2)
		{
			IMessageType* obj = ObjectFactory::Create(message.type);
			obj->FromByteArray(message.body);
			int id = ((M_Delivered*)obj)->GetMessageId();
			if (((Messaging*)parent())->outbox.count(id) > 0)
			{
				((Messaging*)parent())->outbox[id].delivered = true;
			}
			emit ((Messaging*)parent())->MessageDelivered(id);

			((Messaging*)parent())->Log(QString(
					"<font color=green>Message #%1 delivered to %2.</font>").arg(id).arg(((M_Delivered*)obj)->GetFrom().ToString()), 1);
		}
		else
		{
			IMessageType* obj = ObjectFactory::Create(message.type);
			obj->FromByteArray(message.body);
			((Messaging*)parent())->inbox.insert(message.Id(), Message(message));
            emit ((Messaging*)parent())->NewMessage(((Messaging*)parent())->inbox.count() - 1);
 			delete obj;
			if (message.RequireDeliveryConfirm)
			{
				M_Delivered delivery(message.From, replyId);
				Host to = message.From;
				Message m_delivery = ((Messaging*)parent())->ComposeMessage(
					&delivery, to, false, false);
				((Messaging*)parent())->SendMessage(m_delivery);
			}
		}
	}
}

// WriterSocket ----------------------------------------------------------------
WriterSocket::WriterSocket(Messaging *parent, Host destination): AbstractSocket()
{
	setParent(parent);
	dest = destination;
	connect(socket, SIGNAL(connected()), this, SLOT(sendMail()));
	connect(socket, SIGNAL(disconnected()), this, SLOT(Disconnected()));
}

void WriterSocket::Connect()
{
	QAbstractSocket::SocketState sc = socket->state();
	if (sc != QAbstractSocket::ConnectedState)
	{
		socket->connectToHost(dest.IP, dest.Port);
	}
	else
	{
		sendMail();
	}
}

void WriterSocket::Disconnect()
{
	socket->disconnectFromHost();
}

void WriterSocket::Disconnected()
{
	if (((Messaging*)parent())->outSockets.count(dest) > 0)
	{
		((Messaging*)parent())->outSockets.remove(dest);
	}
}

void WriterSocket::sendMail()
{
	Messaging* Conn = (Messaging*)parent();
	while (!Conn->outgoing[this->dest].empty())
	{
		Message m = Conn->outgoing[this->dest].head();
		QByteArray block;
		QDataStream out(&block, QIODevice::WriteOnly);
		out.setVersion(QDataStream::Qt_4_3);
		qint8 flags = 0;
		if (m.RequireDeliveryConfirm) flags += 128;
		if (m.RequireProcessingConfirm) flags += 64;
		qint16 port = ((Messaging*)parent())->Local.Port;
		QByteArray md5 = QCryptographicHash::hash(m.body, QCryptographicHash::Md5);
		out << quint8(PROTOCOL_VERSION) << quint8(flags)
				<< qint8(m.type) << qint32(m.Id()) << qint16(port) << md5
				<< qint32(m.body.length()) << m.body;
		socket->write(block);
		socket->waitForBytesWritten(1000);
		if (m.StoreInBox)
		{
			Conn->outbox.insert(m.Id(), m);
		}
		((Messaging*)parent())->Log(QString("<font color=green>Message to %1:%2 sent.</font>").
									arg(m.To.IP).arg(m.To.Port), 2);
		Conn->outSocketsMutex.lock();
		Conn->outgoing[this->dest].pop_front();
		Conn->outSocketsMutex.unlock();
	}
}
