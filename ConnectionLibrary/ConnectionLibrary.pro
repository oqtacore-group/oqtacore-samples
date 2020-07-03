# ----------------------------------------------------
# This file is generated by the Qt Visual Studio Add-in.
# ------------------------------------------------------

TEMPLATE = lib
TARGET = ConnectionLibrary
DESTDIR = ../Binaries
QT += core network
CONFIG += debug
DEFINES += CONNECTIONLIBRARY_LIBRARY
INCLUDEPATH += ../../../../$(QTDIR)/include/QtCore \
    ../../../../$(QTDIR)/include/QtNetwork \
    ../../../../$(QTDIR)/include \
    ../../../../$(QTDIR)/include/ActiveQt \
    ../../../../debug \
    $(QTDIR)/mkspecs/default

DEPENDPATH += .


HEADERS += \
    Version.h \
    Tests.h \
    Sockets.h \
    Messaging.h \
    Host.h \
    Classes.h

SOURCES += \
    Version.cpp \
    Tests.cpp \
    Sockets.cpp \
    Messaging.cpp \
    Host.cpp \
    Classes.cpp