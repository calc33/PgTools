@ECHO OFF

REM 環境変数SIGNには sign.bat のパスをセットする
SET SIGN=C:\bin\sign.bat

SET DBG=bin\Debug\ExpTbl.exe
SET REL=bin\Release\ExpTbl.exe
IF NOT EXIST %REL% (
  ECHO "Releaseでビルドされていません"
  GOTO END
)
IF NOT EXIST %DBG% GOTO COPY
FOR %%a IN ( %DBG% ) DO SET TDBG=%%~ta
FOR %%b IN ( %REL% ) DO SET TREL=%%~tb

IF "%TDBG%" GTR "%TREL%" (
  ECHO "最後のビルドがDebugで行われています。Releaseでビルドしてください。"
  GOTO END
)

:COPY
CMD /C %SIGN% bin\Release\ExpTbl.exe
XCOPY /S /Y /EXCLUDE:ignore.txt bin\Release\* ..\Release\

:END
PAUSE