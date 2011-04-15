rem - save gpu-image into zip
setlocal

set dd=%date:~6,4%-%date:~3,2%-%date:~0,2%
set zz="C:\Program Files\7-Zip\7z.exe"
set fid=".\gpu-image-%dd%.zip"
set dir2="h:\temptemp\"
set fid2=%dir2%\gpu-image-%dd%.zip"

del /q /s obj/*
"D:\Program Files\Microsoft Visual Studio 9.0\Common7\IDE\devenv.exe" image.csproj /rebuild

erase %fid%
%zz% a %fid% *.ifs
%zz% a %fid% Content -x!Content\.svn -x!bin -x!Content\obj
pushd bin\x86\debug
%zz% a ..\..\..\%fid% Image.exe
popd

copy %fid% %fid2%
pushd %dir2%
rmdir /q /s gpu-image
md gpu-image
cd gpu-image
%zz% x %fid2%
start image.exe
popd
 