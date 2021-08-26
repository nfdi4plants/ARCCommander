@echo off
dotnet fake build -t publishBinariesWin
echo DONE!
timeout 5 >nul