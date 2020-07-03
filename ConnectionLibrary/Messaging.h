#ifndef MESSAGING_H
#define MESSAGING_H
#include "ConnectionLibrary_global.h"
#include <QByteArray>
#include <QTcpServer>
#include <QMap>
#include "Sockets.h"
#include "Classes.h"
#include "Version.h"
#include <QQueue>
#include <QMutex>
#include <QtCore/qtconcurrentexception.h>

class CONNECTION_LIBRARY Message
{
    friend class Connection;
    friend class WriterSocket;
    friend class ReaderSocket;
public:
    Message(IMessageType* object = NULL, const Host& From = Host::Localhost(),
            const Host& To = Host::Localhost(), bool confirmDelivery = false,
            bool confirmProcessing = false, bool store = false);
    Host From;
    Host To;
    bool RequireDeliveryConfirm;
    bool RequireProcessingConfirm;
    bool StoreInBox;
    bool Delivered() const { return delivered; }
    bool Processed() const { return processed; }
    int Id() const {return id; }
    unsigned char Type() const { return type; }
    const QByteArray& Body() const { return body; }
protected:
    int id;
    unsigned char protocolVersion;
    unsigned char type;
    bool delivered;
    bool processed;
    QByteArray body;
    static int lastID;
};

// Class Connection -----------------------------------------------------------
class CONNECTION_LIBRARY Messaging: public QObject
{
    Q_OBJECT
    friend class AbstractSocket;
    friend class WriterSocket;
    friend class ReaderSocket;
    friend class TestSocket;
private:
    QMap<int, Message> inbox;
    QMap<int, Message> outbox;
    QMap<Host, QQueue<Message> > outgoing;
    QMutex outgoingMutex;
    QTcpServer* tcpServer;
    Host Local;	// IP & port we are listening on
    QMap<Host, WriterSocket*> outSockets;
    QMutex outSocketsMutex;
protected:
    bool Log(const QString& logMessage, int logLevel = 1) const;
public:
    Messaging(int logLevel = 0);
    void Connect(const QString& IP, unsigned short Port);
    void Disconnect(const QString& IP, unsigned short Port);
    bool Listen(const QString& IP, unsigned short port);
    Message ComposeMessage(IMessageType* object, const Host& To, bool confirmDelivery = false,
                           bool confirmProcessing = false, bool storeInBox = false) const;
    bool SendMessage(Message message);
    const QMap<int, Message>& Inbox() const;
    void ClearInbox();
    const QMap<int, Message>& Outbox() const;
    void ClearOutbox();
    int LogLevel;	
    void tuneSocketParams(int packetSize, double packetLossProbability, double byteChangeProbability);
signals:
    void NewMessage(int messageID);
    void NewLogMessage(QString message) const;
    void MessageDelivered(int messageID);
    void Error(QString error);
private slots:
    void incConnected();
    void disconnected();
};

#endif // MESSAGING_H
