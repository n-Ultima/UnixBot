git pull
dotnet build -c Release /warnaserror
cd Unix/bin/Release/net6.0
dotnet Unix.dll
