#include "Classes.h"
#include <QVariant>
#include <QDebug>
#include <QTextCodec>
#include <iostream>
#include <QString>

using namespace std;

#define DEBUG

#pragma region IMessageType

qint8 IMessageType::GetType()
{
	return 0;
}

QByteArray IMessageType::ToByteArray()
{
	QByteArray res;
	QDataStream out(&res, QIODevice::WriteOnly);
	out.setVersion(QDataStream::Qt_4_3);
	out << intList << stringList << byteArrayList;
	return res;
}

void IMessageType::FromByteArray(QByteArray arr)
{
	QDataStream in(&arr, QIODevice::ReadOnly);
	in.setVersion(QDataStream::Qt_4_3);
	in >> intList >> stringList >> byteArrayList;
}

QString IMessageType::ToString()
{
	QString temp = QString("Message type: %1\n\n").arg(this->GetType());
	if (intList.size() == 0)
		temp += "Integer List is empty\n";
	else
	{
		temp += "Integer List:\n";
		for (int i = 0; i < intList.size(); i++)
		{
			temp += QString("%1) %2\n").arg(i).arg(intList[i]);
		}
		temp += "\n";
	}
	if (stringList.size() == 0)
		temp += "String List is empty\n";
	else
	{
		temp += "String List:\n";
		for (int i = 0; i < stringList.size(); i++)
		{
			temp += i + ") " + stringList[i] + "\n";
		}
		temp += "\n";
	}
	if (byteArrayList.size() == 0)
		temp += "Byte Array List is empty\n";
	else
	{
		temp += "Byte Array List:\n";
		for (int i = 0; i < byteArrayList.size(); i++)
		{
			temp += QString("%1) %2 bytes\n").arg(i).arg(byteArrayList[i].size());
		}
		temp += "\n";
	}
	return temp;
}

#pragma endregion

#pragma region ObjectFactory

std::map<qint8,  fCreate> ObjectFactory::creators;

bool ObjectFactory::Registered(qint8 id)
{
	return (creators.count(id) > 0);
}

bool ObjectFactory::RegisterType(qint8 id, fCreate func)
{
    if (Registered(id)) return false;
	creators[id] = func;
    return true;
}

bool ObjectFactory::UnregisterType(qint8 id)
{
    if (!Registered(id)) return false;
	creators.erase(id);
	return true;
}

IMessageType* ObjectFactory::Create(qint8 id)
{
    if (!Registered(id))
        throw ConnectionLibrary_InvalidMessageTypeException();
	return creators[id]();
}

void ObjectFactory::RegisterKnownTypes()
{
	RegisterType(1, &M_Log::Create);
	RegisterType(2, &M_Delivered::Create);
	RegisterType(3, &M_File::Create);
	RegisterType(4, &M_FileInfo::Create);
	RegisterType(5, &M_RequestData::Create);
	RegisterType(6, &M_FileList::Create);
	RegisterType(7, &M_MailItems::Create);
}

#pragma endregion

#pragma region M_Log

qint8 M_Log::GetType()
{
	return 1;
}

QString M_Log::GetText()
{
	return stringList[0];
}

void M_Log::SetText(QString Text)
{
	stringList[0] = Text;
}

QString M_Log::ToString()
{
	return stringList[0];
}

IMessageType* M_Log::Create()
{
	return new M_Log();
}

#pragma endregion

#pragma region M_Delivered

qint8 M_Delivered::GetType()
{
	return 2;
}

Host M_Delivered::GetFrom()
{
	return Host(stringList[0], intList[0]);
}

void M_Delivered::SetFrom(Host From)
{
	stringList[0] = From.IP;
	intList[0] = From.Port;
}

int M_Delivered::GetMessageId()
{
	return intList[1];
}

void M_Delivered::SetMessageId(int MessageId)
{
	intList[1] = MessageId;
}

QString M_Delivered::ToString()
{
	return QString("%1:%2 - #%3").arg(stringList[0]).arg(intList[0]).arg(intList[1]);
}


IMessageType* M_Delivered::Create()
{
    ////qDebug() << "IMessageType* M_Delivered::Create()";
	return new M_Delivered(Host::Localhost(), 0);
}

#pragma endregion

#pragma region M_File

qint8 M_File::GetType()
{
	return 3;
}

QString M_File::GetName()
{
	return stringList[0];
}

void M_File::SetName(QString Name)
{
	stringList[0] = Name;
}

QByteArray M_File::GetFile()
{
	return byteArrayList[0];
}

void M_File::SetFile(QByteArray File)
{
	byteArrayList[0] = File;
}

QString M_File::ToString()
{
	QString tempString = "Name :" + stringList[0] + "\n Size: " + byteArrayList[0].length() + "\n";
	return tempString;
}

IMessageType* M_File::Create()
{
	QByteArray byteArray = QByteArray();
	return new M_File("", byteArray);
}

#pragma endregion

#pragma region M_FileInfo

qint8 M_FileInfo::GetType()
{
	return 4;
}

QString M_FileInfo::GetName()
{
	return stringList[0];
}

void M_FileInfo::SetName(QString Name)
{
	stringList[0] = Name;
}

QStringList M_FileInfo::GetTags()
{
	QStringList temp = stringList;
	temp.removeAt(0);
	return temp;
}

void M_FileInfo::SetTags(QStringList Tags)
{
	QStringList temp;
	temp.append(stringList[0]);
	foreach(QString tag, Tags)
	{
		temp.append(tag);
	}
	stringList = temp;
}

QString M_FileInfo::ToString()
{
	return QString("%1: %2 tags").arg(stringList[0]).arg(stringList.count() - 1);
}

IMessageType* M_FileInfo::Create()
{
	return new M_FileInfo();
}

#pragma endregion

#pragma region M_RequestData

qint8 M_RequestData::GetType()
{
	return 5;
}

QString M_RequestData::ToString()
{
	QString result;
	result = "Type: " + QString::number(GetRequestType()) + QString(" intlist: ");
	foreach (int number, intList)
	{
		result =  result  + QString::number(number) + ", ";
	}
	result.chop(2);
	result = result + " stringList: ";
	foreach (QString text, stringList)
	{
		result =  result + text + ", ";
	}
	result.chop(2);
	return result;
}

IMessageType* M_RequestData::Create()
{
	return new M_RequestData(RequestSimilar);
}

#pragma region requestType
M_RequestData::RequestType M_RequestData::GetRequestType()
{
	return static_cast<M_RequestData::RequestType>(intList[0]);
}

void M_RequestData::SetRequestType(M_RequestData::RequestType requestType)
{
	intList[0] = static_cast<int>(requestType);
}

M_RequestData::ChangeType M_RequestData::getChangeType()
{	
	if (GetRequestType() == SubmitChange) 
	{
		return static_cast<ChangeType>(intList[1]);
	}
	else
	{
		throw MRequestData_NoSuchParameterException();
	}
}

void M_RequestData::setChangeType(M_RequestData::ChangeType changeType)
{
	if (GetRequestType() == SubmitChange) 
	{
		intList[1] = static_cast<int>(changeType);
	}
	else
	{
		throw MRequestData_NoSuchParameterException();
	}
}

#pragma endregion
#pragma region login and password
QString M_RequestData::getLoginPassword()
{
	if (GetRequestType()  == RequestAutorization)
	{
		return stringList[0];
	}
	else 	
	{
		throw MRequestData_NoSuchParameterException();
	}
}

void M_RequestData::setLoginPassword(QString loginPassword)
{
	if (GetRequestType()  == RequestAutorization)
	{
		stringList[0] = loginPassword;
	}
	else 	
	{
		throw MRequestData_NoSuchParameterException();
	}
}

QString M_RequestData::getAuthResult()
{
	if (GetRequestType()  == AuthorizationResult)
	{
		return stringList[0];
	}
	else 	
	{
		throw MRequestData_NoSuchParameterException();
	}
}

void M_RequestData::setAuthResult( QString result )
{
	if (GetRequestType()  == RequestAutorization)
	{
		stringList[0] = result;
	}
	else 	
	{
		throw MRequestData_NoSuchParameterException();
	}
}

#pragma endregion
#pragma region fileName
QString M_RequestData::GetFileName()
{
	if ((GetRequestType() == RequestAutorization) || (GetRequestType() == AuthorizationResult))
    {
		throw MRequestData_NoSuchParameterException(); 
	}
	else
    {
		if (stringList.length() < 3) return "";
		return stringList[2];
	}
}

void M_RequestData::SetFileName(QString FileName)
{
	if ((GetRequestType() == RequestAutorization) || (GetRequestType() == AuthorizationResult))
    {
		throw MRequestData_NoSuchParameterException(); 
	}
	else
    {
		stringList[2] = FileName;
	}
}
#pragma endregion

QStringList M_RequestData::GetFileList()
{
	if ((GetRequestType() == RequestAutorization) || (GetRequestType() == AuthorizationResult))
	{
		throw MRequestData_NoSuchParameterException(); 
	}
	else
	{
		if (stringList.length() < 3) return QStringList();
		return stringList.mid(2);
	}
}

void M_RequestData::SetFileList(QStringList& files)
{
	if ((GetRequestType() == RequestAutorization) || (GetRequestType() == AuthorizationResult))
	{
		throw MRequestData_NoSuchParameterException(); 
	}
	else
	{
		stringList = stringList.mid(0,2);
		stringList.append(files);
	}
}
#pragma region userToken
QString M_RequestData::getUserToken()
{
	if (GetRequestType() == RequestAutorization)
    {
		throw MRequestData_NoSuchParameterException(); 
	}
	else
    {
		return stringList[0];
	}
}
void M_RequestData::setUserToken( QString userToken )
{
	if (GetRequestType()  == RequestAutorization)
	{
		throw MRequestData_NoSuchParameterException(); 
	}
	else
	{
		stringList[0] = userToken;
	}
}
#pragma endregion
#pragma region pluginName
QString M_RequestData::getPluginName()
{
	if ((GetRequestType() == RequestAutorization) || (GetRequestType() == AuthorizationResult))
	{
		throw MRequestData_NoSuchParameterException(); 
	}
	else
	{
		return stringList[1];
	}
}

void M_RequestData::setPluginName( QString pluginName )
{
	if ((GetRequestType() == RequestAutorization) || (GetRequestType() == AuthorizationResult))
	{
		throw MRequestData_NoSuchParameterException(); 
	}
	else
	{
		stringList[1] = pluginName;
	}
}

#pragma endregion
#pragma region sendToServer
bool M_RequestData::getSendToServer()
{
	return false;
}
void M_RequestData::setSendToServer( bool sendToServer )
{
}

#pragma endregion
#pragma region maxRequestSize

int M_RequestData::getMaxRequestSize()
{
	if ((GetRequestType() == RequestSimilar) || (GetRequestType() == RequestContent) || (GetRequestType() == RequestFileInfo))
	{
		return intList[1];
	}
	else
	{
		throw MRequestData_NoSuchParameterException();
	}
}

void M_RequestData::setMaxRequestSize( int maxRequestSize )
{
	if ((GetRequestType() == RequestSimilar) || (GetRequestType() == RequestContent) || (GetRequestType() == RequestFileInfo))
	{
		intList[1] = maxRequestSize;
	}
	else
	{
		throw MRequestData_NoSuchParameterException();
	}
}

#pragma endregion
#pragma endregion
#pragma region M_FileList

qint8 M_FileList::GetType()
{
	return 6;
}

QString M_FileList::GetSimilarFileName()
{
	return stringList[0];
}

void M_FileList::SetSimilarFileName(QString SimilarFileName)
{
	stringList[0] = SimilarFileName;
}

QStringList M_FileList::GetFiles()
{
	QStringList temp = stringList;
	temp.removeAt(0);
	return temp;
}

void M_FileList::SetFiles(QStringList Files)
{
	QStringList temp;
	temp.append(stringList[0]);
	foreach(QString file, Files)
	{
		temp.append(file);
	}
	stringList = temp;
}

QString M_FileList::ToString()
{
	return QString("%1: %2 files").arg(stringList[0]).arg(stringList.count() - 1);
}

IMessageType* M_FileList::Create()
{
	return new M_FileList();
}

#pragma endregion

#pragma region M_MailItems

qint8 M_MailItems::GetType()
{
	return 7;
}

QStringList M_MailItems::GetEntryIDArray()
{
	QStringList temp = stringList;
	for(int i = (stringList.count() / 2); i < (stringList.count()); i++)
	{
		temp.removeAt(i);
	}
	return temp;
}

void M_MailItems::SetEntryIDArray(QStringList EntryIDArray)
{
	for(int i = 0; i < EntryIDArray.count(); i++)
	{
		stringList[i] = EntryIDArray[i];
	}
}

QStringList M_MailItems::GetMailItems()
{
	QStringList temp = stringList;
	for(int i = 0; i < (stringList.count() / 2); i++)
	{
		temp.removeAt(i);
	}
	return temp;
}

void M_MailItems::SetMailItems(QStringList MailItems)
{
	for(int i = MailItems.count(); i < MailItems.count() * 2; i++)
	{
		stringList[i] = MailItems[i];
	}
}

IMessageType* M_MailItems::Create()
{
	return new M_MailItems();
}

#pragma endregion
