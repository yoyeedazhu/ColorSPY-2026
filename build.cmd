@echo off
setlocal
set "ROOT=%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo [!] .NET Framework 4.x compiler not found.
  exit /b 1
)
if not exist "%ROOT%build" mkdir "%ROOT%build"

echo [1/2] Building icon...
"%CSC%" /nologo /target:exe /out:"%ROOT%build\IconMaker.exe" "%ROOT%tools\IconMaker.cs" /r:System.dll /r:System.Drawing.dll
if errorlevel 1 exit /b 1
"%ROOT%build\IconMaker.exe" "%ROOT%build\ColorSPY.ico"
if errorlevel 1 exit /b 1

echo [2/2] Building ColorSPY2026.exe...
"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 ^
  /win32manifest:"%ROOT%src\app.manifest" ^
  /win32icon:"%ROOT%build\ColorSPY.ico" ^
  /out:"%ROOT%ColorSPY2026.exe" ^
  "%ROOT%src\*.cs" ^
  /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll
if errorlevel 1 exit /b 1

echo.
echo Built: %ROOT%ColorSPY2026.exe
endlocal
