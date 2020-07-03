#include "Host.h"
#include <QDataStream>
#define localIP "127.0.0.1"
#define localPort 12345

Host::Host(QString ip, unsigned short port): IP(ip), Port(port) {}

bool Host::operator ==(const Host& rhs) const
{
	return (this->IP == rhs.IP) && (this->Port == rhs.Port);
}

bool Host::operator <(const Host& rhs) const
{
	if (this->IP < rhs.IP) return true;
	if ((this->IP == rhs.IP) && (this->Port < rhs.Port)) return true;
	return false;
}

Host Host::Localhost()
{
	Host h;
	h.IP = localIP;
	h.Port = localPort;
	return h;
}

QString Host::ToString() const
{
	return QString("%1:%2").arg(IP).arg(Port);
}

QDataStream& operator << (QDataStream& stream, const Host& host)
{
	stream << host.IP << host.Port;
	return stream;
}

QDataStream& operator >> (QDataStream& stream, Host& host)
{
	stream >> host.IP >> host.Port;
	return stream;
}


