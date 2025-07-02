@echo off
echo Building BGM Selector for Persona 3 Reload...

REM Create build directory if it doesn't exist
if not exist build mkdir build

REM Build the application
echo Building application...
dotnet publish BGMSelector.csproj -c Release -o build

echo Done! You can now run BGMSelector.exe from the build directory. 