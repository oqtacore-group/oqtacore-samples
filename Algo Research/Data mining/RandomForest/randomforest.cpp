#include "randomforest.h"
#include <QObject>
#include <QVector>
#include <QSet>
#include <QMap>
#include <QString>
#include <QDir>
#include <QList>
#include <QTextCodec>
#include <QProcess>

const QString TERM_PARSER_ANALYZER = "..\\AlgoPy\\TermParser.py";
const QString RUNNER = "..\\AlgoPy\\runner.py";

RandomForest::RandomForest()
{
}

void RandomForest::analyzeFiles( QStringList filenames, QMap<QString, QStringList> &tagsList, QMap<QString, QStringList> &similarFiles )
{
	//First, plan of the algorithm:
	// 1.We get all the names of the files (we have them already)
	// 2.We look if some of the files are not available. We need to get them (at least, their text)
	// 3.We get terms out of files
	// 4.We get frequencies of all the terms in each of all the files
	// 5.We pass all these parameters to Random Forest and let it do all it wants
	QString temp;
	QTextCodec* codec1;
	codec1->codecForName("UTF8");
	//First we try to get text from files
	foreach(QString filename, filenames)
	{
		QString fileText;
		emit fileNeeded(fileText);
		QFile tempFile("temp.txt");
		tempFile.open(QIODevice::Text | QIODevice::WriteOnly);
		//launch term parser
		QProcess* proc = new QProcess;
		QStringList args;
		args.push_back(QDir::toNativeSeparators().replace("\\", "/"));
		args.push_back(QDir::toNativeSeparators(it).replace("\\", "/") + PARSED_FILE_EXTENSION);
		args.push_back("false");
		proc->start(TERM_PARSER_ANALYZER, args);
		proc->waitForFinished();
	}