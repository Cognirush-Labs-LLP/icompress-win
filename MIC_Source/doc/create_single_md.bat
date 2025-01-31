@echo off
REM Batch script to combine all .md files in the current directory into combined.md

REM Define the name of the combined file
set "COMBINED_FILE=combined.md"

REM Check if the combined file already exists and delete it to avoid duplication
if exist "%COMBINED_FILE%" del "%COMBINED_FILE%"

REM Loop through all .md files in the directory
for %%f in (*.md) do (
    REM Check if the current file is not the combined file
    if /i not "%%f"=="%COMBINED_FILE%" (
        echo Adding %%f to %COMBINED_FILE%
        type "%%f" >> "%COMBINED_FILE%"
        REM Optionally, add a newline or separator between files
        echo. >> "%COMBINED_FILE%"
    )
)

echo All .md files have been combined into %COMBINED_FILE%.
pause
