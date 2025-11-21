@echo off
REM this is to save ProgramData as ProgamDataOLD 

set "program_data=%TARILIO-Portable\\ProgramData"
set "program_data_old=%TARILIO-Portable\\ProgramDataOLD"

if exist "%program_data%" (
        ren "%program_data%" "ProgramDataOLD"
        echo Renamed ProgramData to ProgramDataOLD
    )
) else (
    echo %program_data% does not exist
)
