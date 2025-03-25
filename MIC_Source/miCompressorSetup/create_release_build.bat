@echo off
setlocal
SET DIST_DIR=dist


echo Cleaning dist directory...
IF EXIST %DIST_DIR% (
    rmdir /s /q %DIST_DIR% || exit /b 1
)
mkdir %DIST_DIR% || exit /b 1

PUSHD ..\miCompressor || exit /b 1

dotnet clean || exit /b 1
dotnet build -c Release || exit /b 1
dotnet publish -c Release -r win-x64 --self-contained true -o ../miCompressorSetup/dist || exit /b 1

POPD

echo ****Build and publish completed successfully****