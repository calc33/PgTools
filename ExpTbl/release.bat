@ECHO OFF

REM ���ϐ�SIGN�ɂ� sign.bat �̃p�X���Z�b�g����
SET SIGN=C:\bin\sign.bat

SET DBG=bin\Debug\ExpTbl.exe
SET REL=bin\Release\ExpTbl.exe
IF NOT EXIST %REL% (
  ECHO "Release�Ńr���h����Ă��܂���"
  GOTO END
)
IF NOT EXIST %DBG% GOTO COPY
FOR %%a IN ( %DBG% ) DO SET TDBG=%%~ta
FOR %%b IN ( %REL% ) DO SET TREL=%%~tb

IF "%TDBG%" GTR "%TREL%" (
  ECHO "�Ō�̃r���h��Debug�ōs���Ă��܂��BRelease�Ńr���h���Ă��������B"
  GOTO END
)

:COPY
CMD /C %SIGN% bin\Release\ExpTbl.exe
XCOPY /S /Y /EXCLUDE:ignore.txt bin\Release\* ..\Release\

:END
PAUSE