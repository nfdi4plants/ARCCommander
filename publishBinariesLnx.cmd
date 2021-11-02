@echo off
dotnet fake build -t publishBinariesLinux
echo DONE!
timeout 5 >nul