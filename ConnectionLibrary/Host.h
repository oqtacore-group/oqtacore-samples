#ifndef HOST_H
#define HOST_H
#include "ConnectionLibrary_global.h"
#include <QString>
#define defaultIP "0.0.0.0"
#define defaultport 21894

class CONNECTION_LIBRARY Host
{
public:
	QString IP;
	unsigned short Port;
	Host(QString ip = defaultIP, unsigned short port = defaultport);
	bool operator ==(const Host& rhs) const;
	bool operator <(const Host& rhs) const;
	static Host Localhost();	
	QString ToString() const;
};

QDataStream& operator << (QDataStream& stream, const Host& host);
QDataStream& operator >> (QDataStream& stream, Host& host);
#endif // HOST_H
