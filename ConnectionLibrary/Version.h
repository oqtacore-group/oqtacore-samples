#ifndef VERSION_H
#define VERSION_H

#include "ConnectionLibrary_global.h"
 #include <QDataStream>
#include <qtconcurrentexception.h>

const unsigned char PROTOCOL_VERSION = 2;

class CONNECTION_LIBRARY InvalidVersion : public QtConcurrent::Exception {};

QDataStream::Version StreamVersion(unsigned char ProtocolVersion);

#endif // VERSION_H
