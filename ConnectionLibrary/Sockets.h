#ifndef SOCKETS_H
#define SOCKETS_H
#include "ConnectionLibrary_global.h"
#include <QTcpSocket>
#include "Host.h"
#include <QtCore/qtconcurrentexception.h>
#include "Tests.h"
#include <QBitArray>
#include "classes.h"

class CONNECTION_LIBRARY Messaging;	// forward declaration
class TestSocket;	// forward declaration
class CONNECTION_LIBRARY CorruptedHashException:  public QtConcurrent::Exception {};

#ifdef DEBUG
	typedef TestSocket TCP_SOCKET;
#else
	typedef QTcpSocket TCP_SOCKET;
#endif

// AbstractSocket ----------------------------------------------------------------
class CONNECTION_LIBRARY AbstractSocket: public QObject
{
	Q_OBJECT
public:
	AbstractSocket();
	QAbstractSocket::SocketState State() const;
	Host Peer() const;
	Host Local() const;
	static QString BitsToString(QByteRef Byte);
	void tuneSocketParams(int packetSize, double packetLossProbability, double byteChangeProbability);
	void outputByteArray(QByteArray byteArray);
protected:
	TCP_SOCKET* socket;
protected slots:
	void displayError(QAbstractSocket::SocketError socketError);
	void onDisconnect();
signals:
	void disconnected();
};

// ReaderSocket ----------------------------------------------------------------
class CONNECTION_LIBRARY ReaderSocket: public AbstractSocket
{
	Q_OBJECT
public:
	ReaderSocket(Messaging *parent, QTcpSocket* sock = 0);
public slots:
	void read();
private:
	qint8 protocol, flags, type;
	qint16 port;
	qint32 replyId;
	quint32 nextBlockSize;
};

// WriterSocket ----------------------------------------------------------------
class CONNECTION_LIBRARY WriterSocket: public AbstractSocket
{
	Q_OBJECT
public:
	WriterSocket(Messaging *parent, Host destination);
	void Connect();
	void Disconnect();
protected slots:
	void sendMail();
	void Disconnected();
private:
	Host dest;
};

#endif // SOCKETS_H
