#-------------------------------------------------
#
# Project created by QtCreator 2012-09-18T21:08:42
#
#-------------------------------------------------

QT       += network

QT       -= gui

TARGET = RandomForest
TEMPLATE = lib

DEFINES += RANDOMFOREST_LIBRARY

SOURCES += randomforest.cpp

HEADERS += randomforest.h\
        RandomForest_global.h

symbian {
    MMP_RULES += EXPORTUNFROZEN
    TARGET.UID3 = 0xE032131F
    TARGET.CAPABILITY = 
    TARGET.EPOCALLOWDLLDATA = 1
    addFiles.sources = RandomForest.dll
    addFiles.path = !:/sys/bin
    DEPLOYMENT += addFiles
}

unix:!symbian {
    maemo5 {
        target.path = /opt/usr/lib
    } else {
        target.path = /usr/lib
    }
    INSTALLS += target
}
