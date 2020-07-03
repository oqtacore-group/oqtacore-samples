#include "Tests.h"
#include <QHostAddress>
#include "Messaging.h"

TestSocket::TestSocket(QObject* parent)
{
	tuneSocketParams();
}

qint64 TestSocket::write(const QByteArray &byteArray)
{
	QDataStream& d = ((TestSocket*)(((Messaging*)(parent()->parent()))->outSockets[dest]))->data;
	QDataStream in(byteArray);
	qint8 c;
	qint64 bytesWritten;
	while (!in.atEnd())
	{
		bool toSkip = (double)rand() / RAND_MAX < packetLossProbability;
		for (int i = 0; i < packetSize && !in.atEnd(); ++i)
		{
			in >> c;
			if (!toSkip)
			{
				if ((double)rand() / RAND_MAX < byteChangeProbability)
				{
					c = (c + rand()) % 256;
				}
				d << c;
			}
			++bytesWritten;
		}
	}
	emit ((TestSocket*)(((Messaging*)(parent()->parent()))->outSockets[dest]))->readyRead();
	return bytesWritten;
}

QAbstractSocket::SocketState TestSocket::state()
{
	return st;
}

void TestSocket::connectToHost(const QString& hostName, quint16 port,
							   OpenMode openMode)
{
	dest = Host(hostName, port);
	st = QAbstractSocket::ConnectedState;
	emit stateChanged(st);
	emit connected();
}

void TestSocket::connectToHost(const QHostAddress& address, quint16 port,
							   OpenMode openMode)
{
	connectToHost(address.toString(), port, openMode);
}

void TestSocket::disconnectFromHost()
{
	st = QAbstractSocket::UnconnectedState;
	emit stateChanged(st);
	emit disconnected();
}

QHostAddress TestSocket::peerAddress() const
{
    return QHostAddress(dest.IP);
}

quint16 TestSocket::peerPort() const
{
	return dest.Port;
}

TestSocket::operator QDataStream & ()
{
	return data;
}

void TestSocket::tuneSocketParams(int packetSize, double packetLossProbability, double byteChangeProbability)
{
	this->packetSize = qMax(packetSize, 1);
	this->packetLossProbability = qMin(qMax(packetLossProbability, 0.0), 1.0);
	this->byteChangeProbability = qMin(qMax(byteChangeProbability, 0.0), 1.0);
}
