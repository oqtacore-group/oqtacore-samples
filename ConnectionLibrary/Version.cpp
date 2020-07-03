#include "Version.h"

QDataStream::Version StreamVersion(unsigned char ProtocolVersion)
{
	switch (ProtocolVersion)
	{
	case 1:
		return QDataStream::Qt_4_3;

	default:
		throw InvalidVersion();
	}
}
