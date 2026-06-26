@echo off
title Minori Engine Unpacker (Universal)
echo ===========================================
echo      MINORI ENGINE UNPACKER TOOL
echo ===========================================
echo.

set /p input="Masukkan nama file PAZ (contoh: scr.paz): "
set /p idx="Masukkan nomor Index Game (liat daftar di README): "
set /p folder="Masukkan nama folder hasil (contoh: isi_scr): "

echo.
echo Sedang membongkar %input%...
tools\fuckpaz.exe u "%input%" %idx% "%folder%"

echo.
echo === SELESAI PEUY! Hasil ada di folder: %folder% ===
pause