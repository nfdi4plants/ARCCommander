@echo off
dotnet fake build -t publishBinariesMac
dotnet fake build -t publishBinariesMacARM

echo DONE!
timeout 5 >nul