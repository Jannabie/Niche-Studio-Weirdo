@echo off
set /p folder="Masukkan nama folder script (contoh: AAA): "
set /p idx="Masukkan nomor Index Game (liat daftar di README): "
set /p output="Masukkan nama file hasil (contoh: scr_new.paz): "

echo.
echo Sedang membungkus...
tools\fuckpaz.exe p "%folder%" %idx% "%output%"
echo.
echo === SELESAI PEUY! ===
pause