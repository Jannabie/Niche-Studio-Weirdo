@echo off
chcp 65001 > nul

REM ===== Pengaturan =====
set "MAIN_SCRIPT=cli_launcher.py"
set "OUTPUT_DIR=dist"
set "PYTHON_VER=python"

REM ===== Mulai build =====
echo [INFO] Memulai build...

%PYTHON_VER% -m nuitka ^
--standalone ^
--onefile ^
--run ^
--assume-yes-for-downloads ^
--enable-plugin=multiprocessing ^
--enable-plugin=numpy ^
--include-package=cv2 ^
--include-module=mutagen ^
--include-module=PIL ^
--include-module=tqdm ^
--include-module=pyogg ^
--output-dir=%OUTPUT_DIR% ^
--output-filename=Malie_UnRePacker_Tool_CLI.exe ^
--remove-output ^
--windows-console-mode=force ^
--show-progress ^
--show-memory ^
%MAIN_SCRIPT%

REM ===== Simpan log eksekusi =====
echo [INFO] Build selesai!
if exist "%OUTPUT_DIR%\Malie_UnRePacker_Tool_CLI.exe" (
    echo [INFO] Menjalankan EXE... log disimpan ke runlog.txt
    "%OUTPUT_DIR%\Malie_UnRePacker_Tool_CLI.exe" > runlog.txt 2>&1
) else (
    echo [ERROR] Build gagal! File executable tidak ditemukan.
)

echo [INFO] Periksa runlog.txt.
pause
