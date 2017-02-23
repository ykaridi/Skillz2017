del code.zip
for /r %%i in (*.cs) do find /c "namespace MyBot" "%%i"  && ("C:\Program FIles\7-Zip\7z.exe" a code.zip "%%i")