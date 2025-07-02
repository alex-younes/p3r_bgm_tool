@echo off
echo Building BGM Selector for Persona 3 Reload...

REM Create build directory if it doesn't exist
if not exist build mkdir build

REM Copy required files to build directory
echo Copying required files...
copy music.yaml build\music.yaml
copy global_music.pme build\global_music.pme
if not exist build\p3r mkdir build\p3r
xcopy /Y /I p3r\*.hca build\p3r

REM Build the application
echo Building application...
dotnet publish -c Release -o build

echo Done! You can now run BGMSelector.exe from the build directory. 