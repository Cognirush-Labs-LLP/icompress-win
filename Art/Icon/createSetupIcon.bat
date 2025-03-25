@echo on
setlocal enabledelayedexpansion

:: Set the path to ImageMagick's magick.exe (Change this to match your installation path)
set IMAGEMAGICK_PATH="D:\Downloads\ImageMagick-7.1.1-43-portable-Q16-x64\magick.exe"

:: Input image name
set INPUT_IMAGE=micSetup.png
set OUTPUT_ICON=mic_win_4_setup.ico

:: Check if ImageMagick exists
if not exist %IMAGEMAGICK_PATH% (
    echo Error: magick.exe not found at %IMAGEMAGICK_PATH%
    exit /b 1
)

:: Create output folder
if not exist "setup_output" mkdir setup_output

:: Define required icon sizes
for %%S in (16 20 24 32 40 48 60 64 72 96 128 256) do (
    %IMAGEMAGICK_PATH% %INPUT_IMAGE% -resize %%Sx%%S "setup_output/icon-%%S.png"
    echo Created: setup_output/icon-%%S.png
)

:: Convert to .ico
%IMAGEMAGICK_PATH% setup_output/icon-16.png setup_output/icon-20.png setup_output/icon-24.png setup_output/icon-32.png setup_output/icon-40.png setup_output/icon-48.png setup_output/icon-60.png setup_output/icon-64.png setup_output/icon-72.png setup_output/icon-96.png setup_output/icon-128.png setup_output/icon-256.png %OUTPUT_ICON%

echo Icon created: %OUTPUT_ICON%
exit /b 0
