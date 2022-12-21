@echo off
dotnet fake build -t publishBinariesMacBoth

echo DONE!
timeout 5 >nul