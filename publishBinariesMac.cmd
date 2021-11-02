@echo off
dotnet fake build -t publishBinariesMac
echo DONE!
timeout 5 >nul