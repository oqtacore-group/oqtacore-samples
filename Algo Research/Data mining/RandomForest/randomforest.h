#ifndef RANDOMFOREST_H
#define RANDOMFOREST_H

#include "RandomForest_global.h"
#include "../SmartTaggerServer/Interfaces.h"

class RandomForest: public AlgorithmInterface
{
	Q_OBJECT
public:
    RandomForest();
	~RandomForest(){}

signals:
	void fileNeeded(QString &putTextHere);

	public slots:
		void analyzeFiles(QStringList filenames, QMap<QString, QStringList> &tagsList, QMap<QString, QStringList> &similarFiles); //Storage Plugin will send list of files that need to be analyzed and algoServer has to send back tagsList and similarFilesList
public:
};

#endif // RANDOMFOREST_H
