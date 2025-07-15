@echo off
REM This will ensure old data is used if user has renamed ProgramData to ProgramDataOLD

set "program_data=%eSearchPortable\\ProgramData"
set "program_data_old=%eSearchPortable\\ProgramDataOLD"
set "program_data_new=%eSearchPortable\\ProgramDataNEW"

if exist "%program_data_old%" (
    if exist "%program_data%" (
        ren "%program_data%" "ProgramDataNEW"
        echo Renamed ProgramData to ProgramDataNEW
    )
    ren "%program_data_old%" "ProgramData"
    echo Renamed ProgramDataOLD to ProgramData
) else (
    echo %program_data_old% does not exist
)

start "" /d "eSearchPortable" "eSearch.exe"