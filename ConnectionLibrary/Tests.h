#ifndef TESTS_H
#define TESTS_H
#include "Sockets.h"

class ReaderSocket;	// forward declaration

class TestSocket: public QTcpSocket
{
	Q_OBJECT
public:
	TestSocket(QObject* parent = 0);
	qint64 write(const QByteArray& byteArray);
	QAbstractSocket::SocketState state();
	void connectToHost(const QString& hostName, quint16 port, OpenMode openMode = ReadWrite);
	void connectToHost(const QHostAddress& address, quint16 port, OpenMode openMode = ReadWrite);
	void disconnectFromHost();
	QHostAddress peerAddress() const;
	quint16 peerPort() const;
	operator QDataStream&();
	void tuneSocketParams(int packetSize = 200, double packetLossProbability = 0, double byteChangeProbability = 0);
	friend class ReaderSocket;
	int packetSize;
	double packetLossProbability;
	double byteChangeProbability;
private:
	QDataStream data;
	Host dest;
	SocketState st;
};

#endif // TESTS_H
