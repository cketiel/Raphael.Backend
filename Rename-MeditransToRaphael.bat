@echo off
setlocal enabledelayedexpansion

echo 🚀 Iniciando renombrado de Meditrans a Raphael...

:: Paso 1: Renombrar carpetas
if exist "Meditrans.Api" (
    ren "Meditrans.Api" "Raphael.Api"
    echo ✅ Carpeta renombrada: Meditrans.Api ➜ Raphael.Api
)

if exist "Meditrans.Shared" (
    ren "Meditrans.Shared" "Raphael.Shared"
    echo ✅ Carpeta renombrada: Meditrans.Shared ➜ Raphael.Shared
)

:: Paso 2: Renombrar archivos que contienen "Meditrans" en su nombre
for /r %%f in (*Meditrans*.cs) do (
    set "old=%%~nxf"
    set "new=!old:Meditrans=Raphael!"
    ren "%%~dpfxf" "!new!"
    echo ✅ Archivo renombrado: !old! ➜ !new!
)

:: Paso 3: Reemplazar texto en archivos .cs, .csproj, .sln, .json
echo 📝 Reemplazando 'Meditrans' por 'Raphael' en el contenido de los archivos...

for %%X in (cs csproj sln json) do (
    for /r %%f in (*.%%X) do (
        powershell -Command "(Get-Content -Raw '%%f') -replace 'Meditrans', 'Raphael' | Set-Content '%%f'"
        echo 📝 Actualizado: %%f
    )
)

:: Paso 4: Eliminar carpetas temporales
echo 🧹 Limpiando carpetas bin, obj y .vs...

for /d /r %%d in (bin) do (
    if exist "%%d" rd /s /q "%%d"
)
for /d /r %%d in (obj) do (
    if exist "%%d" rd /s /q "%%d"
)
if exist ".vs" rd /s /q ".vs"

echo ✅ Renombrado completo. Abre Visual Studio y reconstruye la solución.

endlocal
pause
