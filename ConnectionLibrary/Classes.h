#ifndef CLASSES_H
#define CLASSES_H
#include "ConnectionLibrary_global.h"
#include <qstring.h>
#include "Host.h"
#include <map>
#include <QtCore/qtconcurrentexception.h>
#include <QFile>
#include <QStringList>
#include <QTextStream>

class CONNECTION_LIBRARY ConnectionLibrary_InvalidMessageTypeException:  public QtConcurrent::Exception {};
class CONNECTION_LIBRARY MRequestData_NoSuchParameterException:  public QtConcurrent::Exception {};

// IMesssageType - parent of types of message--------------------------------------
class CONNECTION_LIBRARY IMessageType
{
public:

#pragma region main data
	QStringList stringList;
	QList<int> intList;
	QList<QByteArray> byteArrayList;
#pragma endregion

#pragma region standard operators to work with the main data
	virtual qint8 GetType();
	virtual QByteArray ToByteArray();
	virtual QString ToString();
	virtual void FromByteArray(QByteArray);
#pragma endregion

protected:
	IMessageType() {}
};

typedef IMessageType* (*fCreate)();

// Object Factory - responsible for the registration of types of messages----------
class CONNECTION_LIBRARY ObjectFactory
{
public:
	static bool RegisterType(qint8 id, fCreate func);
	static bool UnregisterType(qint8 id);
	static bool Registered(qint8 id);
	static IMessageType* Create(qint8 id);
	static void RegisterKnownTypes();
private:
	ObjectFactory() {}
	static std::map<qint8,  fCreate> creators;
};

// Class 1 - log message ----------------------------------------------------------
class CONNECTION_LIBRARY M_Log: public IMessageType
{
#pragma region constructors
public:
	M_Log(QString text = "")
	{
		stringList.append(text);
	}
#pragma endregion

#pragma region main data instance
	QString GetText();
	void SetText(QString Text);
#pragma endregion

#pragma region standard operators to work with the main data
	QString ToString() override;
	qint8 GetType() override;
	static IMessageType* Create();
#pragma endregion
};

// Class 2 - delivery report ------------------------------------------------------
class CONNECTION_LIBRARY M_Delivered: public IMessageType
{
#pragma region constructors
public:
	M_Delivered(const Host& from, int messageId)
	{
		stringList.append(from.IP);
		intList.append(from.Port);
		intList.append(messageId);
	}
	M_Delivered(QByteArray arr);
#pragma endregion

#pragma region main data instance
	Host GetFrom();
	void SetFrom(Host From);
	int GetMessageId();
	void SetMessageId(int MessageId);
#pragma endregion

#pragma region standard operators to work with the main data
	QString ToString() override;
	qint8 GetType() override;
	static IMessageType* Create();
#pragma endregion
};

// Class 3 - file  ----------------------------------------------------------------
class CONNECTION_LIBRARY M_File: public IMessageType
{
	public:
#pragma region constructors
	M_File(QString Name, QByteArray File)
	{
		stringList.append(Name);
		byteArrayList.append(File);
	}
#pragma endregion

#pragma region main data instance
	QString GetName();
	void SetName(QString Name);
	QByteArray GetFile();
	void SetFile(QByteArray File);
#pragma endregion

#pragma region standard operators to work with the main data
	QString ToString() override;
	qint8 GetType() override;
	static IMessageType* Create();
#pragma endregion

#pragma region internal operators to work with the main data
#pragma endregion
};

// Class 4 - file info ------------------------------------------------------------
class CONNECTION_LIBRARY M_FileInfo: public IMessageType
{
public:
    enum RequestType { RequestFileInfo, RequestContent, RequestSimilar, RequestAutorization, AuthorizationResult, SubmitChange};
#pragma region constructors
	M_FileInfo() {}
	M_FileInfo(const QString& name, const QStringList tags)
	{
		stringList.append(name);
		foreach(QString tag, tags)
		{
			stringList.append(tag);
		}
	}
#pragma endregion

#pragma region main data instance
	QString GetName();
	void SetName(QString Name);
	QStringList GetTags();
	void SetTags(QStringList Tags);
#pragma endregion

#pragma region standard operators to work with the main data
	QString ToString() override;
	qint8 GetType() override;
	static IMessageType* Create();
#pragma endregion

#pragma region internal operators to work with the main data
	//QString GetFileName() const;
	//QString GetFilePath() const;
#pragma endregion
};

// Class 5 - request for some file attributes -------------------------------------
class CONNECTION_LIBRARY M_RequestData: public IMessageType
{
public:
#pragma region internal types
    enum RequestType { RequestFileInfo, RequestContent, RequestSimilar, RequestAutorization, AuthorizationResult, SubmitChange};
	enum ChangeType { AddFiles, AddFolders, DeleteFiles, DeleteFolders, NewFileList };
#pragma endregion

#pragma region constructors
	//************************************
	// FullName:  M_RequestData::M_RequestData
	// Ask for authorization, result of authorization
	//************************************
	M_RequestData(RequestType type/*intList[0]*/, const QString& text = ""/*stringList[0]*/)
	{
		intList.append((int)type);
		stringList.append(text);
	}
	//************************************
	// FullName:  M_RequestData::M_RequestData
	// Ask for tags, similar files, content.
	//************************************
	M_RequestData(	RequestType type/*intList[0]*/, const QString& userToken/*stringList[0]*/, 
					const QString& pluginName/*stringList[1]*/, const QStringList& fileName/*stringList[2]*/,  int maxRequestSize = 0/*intList[1]*/)
	{
		intList.append((int)type);
		intList.append(maxRequestSize);
		stringList.append(userToken);
		stringList.append(pluginName);
		stringList.append(fileName);
	}
	//************************************
	// FullName:  M_RequestData::M_RequestData
	// To tell about added files, folders
	//************************************
	M_RequestData(	ChangeType changeType, const QString& userToken/*stringList[0]*/,
					const QString& pluginName/*stringList[1]*/, const QStringList& fileNames/*stringList[2+]*/)
	{
		intList.append((int)SubmitChange);
		intList.append(changeType);
		stringList.append(userToken);
		stringList.append(pluginName);
		stringList.append(fileNames);
	}
	//************************************
	// FullName:  M_RequestData::M_RequestData
	// Send files and folders. This will be defined be server automatically
	//************************************
	M_RequestData(	RequestType type/*intList[0]*/, const QString& userToken/*stringList[0]*/, 
					const QString& pluginName/*stringList[1]*/, const QStringList& fileName/*stringList[2]*/, bool sendToServer=false/*intList[1]*/)
	{
		intList.append((int)type);
		intList.append((int)sendToServer);
		stringList.append(userToken);
		stringList.append(pluginName);
		stringList.append(fileName);
	}
#pragma endregion

#pragma region main data instance
	RequestType GetRequestType();
	void SetRequestType(RequestType requestType);
	ChangeType getChangeType();
	void setChangeType(ChangeType changeType);
	QString getUserToken();
	void setUserToken(QString userToken);
	QString getPluginName();
	void setPluginName(QString pluginName);
    QString getLoginPassword();
	void setLoginPassword(QString loginPassword);
	QString getAuthResult();
	void setAuthResult(QString result);
	QString GetFileName();
	void SetFileName(QString FileName);
	QStringList GetFileList();
	void SetFileList(QStringList& files);
	bool getSendToServer();
	void setSendToServer(bool sendToServer);
	int getMaxRequestSize();
	void setMaxRequestSize(int maxRequestSize);
#pragma endregion

#pragma region standard operators to work with the main data
	QString ToString() override;
	qint8 GetType() override;
	static IMessageType* Create();
#pragma endregion
};

class RequestFactory
{
public:
	static M_RequestData askForFileInfo(const QString& pluginName, const QString& fileName, int maxTags = 0)
	{
		return M_RequestData(M_RequestData::RequestFileInfo, "Not implemented", pluginName, QStringList(fileName), maxTags);
	}
	static M_RequestData askForContent(const QString& pluginName, const QString& fileName, int maxChars = 0)
	{
		return M_RequestData(M_RequestData::RequestContent, "Not implemented", pluginName, QStringList(fileName), maxChars);
	}
	static M_RequestData askForFileList(const QString& pluginName)
	{
		return M_RequestData(M_RequestData::RequestSimilar, "Not implemented", pluginName,QStringList(""), 0);
	}
	static M_RequestData askForAuthorization(const QString& login, const QString& password)
	{
		return M_RequestData(M_RequestData::RequestAutorization, "login:" + login + "password:" + password);
	}
	static M_RequestData authorizationResult(const QString& token)
	{
		return M_RequestData(M_RequestData::AuthorizationResult, token);
	}
	static M_RequestData filesAdded(const QString& userToken, const QString& plugin, const QStringList& files)
	{
		return M_RequestData(M_RequestData::AddFiles, userToken, plugin, files);
	}
	static M_RequestData filesRemoved(const QString& userToken, const QString& plugin, const QStringList& files)
	{
		return M_RequestData(M_RequestData::DeleteFiles, userToken, plugin, files);
	}
	static M_RequestData foldersAdded(const QString& userToken, const QString& plugin, const QStringList& files)
	{
		return M_RequestData(M_RequestData::AddFolders, userToken, plugin, files);
	}
	static M_RequestData newFilelist(const QString& userToken, const QString& plugin, const QStringList& files)
	{
		return M_RequestData(M_RequestData::NewFileList, userToken, plugin, files);
	}
	static M_RequestData foldersRemoved(const QString& userToken, const QString& plugin, const QStringList& files)
	{
		return M_RequestData(M_RequestData::DeleteFolders, userToken, plugin, files);
	}
};

// Class 6 - list of files --------------------------------------------------------
class CONNECTION_LIBRARY M_FileList: public IMessageType
{
public:
#pragma region constructors
	M_FileList() {}
	M_FileList(const QString& similarFileName, const QStringList files)
	{
		stringList.append(similarFileName);
		foreach(QString file, files)
		{
			stringList.append(file);
		}
	}
#pragma endregion

#pragma region main data instance
	QString GetSimilarFileName();
	void SetSimilarFileName(QString SimilarFileName);
	QStringList GetFiles();
	void SetFiles(QStringList Files);
#pragma endregion

#pragma region standard operators to work with the main data
	QString ToString() override;
	qint8 GetType() override;
	static IMessageType* Create();
#pragma endregion
};

// Class 7 - mail items --------------------------------------------------------
class CONNECTION_LIBRARY M_MailItems : public IMessageType
{
public:
#pragma region constructors
	M_MailItems() {}
	M_MailItems(QStringList EntryIDArray, QStringList MailItems)
	{
		for (int i = 0; i < EntryIDArray.count(); i++)
		{
			stringList.append(EntryIDArray[i]);
		}
		for (int i = 0; i < MailItems.count(); i++)
		{
			stringList.append(MailItems[i]);
		}
	}
#pragma endregion

#pragma region main data instance
	QStringList GetEntryIDArray();
	void SetEntryIDArray(QStringList EntryIDArray);
	QStringList GetMailItems();
	void SetMailItems(QStringList MailItems);
#pragma endregion

#pragma region standard operators to work with the main data
	qint8 GetType() override;
	static IMessageType* Create();
#pragma endregion

};

#endif // CLASSES_H
