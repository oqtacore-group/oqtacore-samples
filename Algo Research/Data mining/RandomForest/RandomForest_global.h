#ifndef RANDOMFOREST_GLOBAL_H
#define RANDOMFOREST_GLOBAL_H

#include <QtCore/qglobal.h>

#if defined(RANDOMFOREST_LIBRARY)
#  define RANDOMFORESTSHARED_EXPORT Q_DECL_EXPORT
#else
#  define RANDOMFORESTSHARED_EXPORT Q_DECL_IMPORT
#endif

#endif // RANDOMFOREST_GLOBAL_H