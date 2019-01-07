@echo off

set _msbuildPath="C:\Program Files\Microsoft Visual Studio\2019\Preview\MSBuild\Current\Bin"

%_msbuildPath%\msbuild "..\src\CommandLine.sln" /t:Build /p:Configuration=Debug /v:m /m

"..\src\CommandLine\bin\Debug\net461\roslynator" lloc "..\src\Roslynator.sln" ^
 --msbuild-path %_msbuildPath% ^
 --verbosity d ^
 --file-log "roslynator.log" ^
 --file-log-verbosity diag

pause
