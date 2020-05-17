@ECHO OFF

MKDIR Release

PUSHD DB2Src.NET
CALL release.bat
POPD

PUSHD ExpSchema
CALL release.bat
POPD

PUSHD ExpTbl
CALL release.bat
POPD
