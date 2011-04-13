rem - save gpu-image into zip

set zz="C:\Program Files\7-Zip\7z.exe"
set fid=".\gpu-image.zip"
set fid2="h:\temptemp\gpu-image.zip"
erase %fid%
%zz% a %fid% *.ifs
%zz% a %fid% Content -x!Content\.svn -x!bin -x!Content\obj
pushd bin\x86\debug
%zz% a ..\..\..\%fid% Image.exe
popd

copy %fid% %fid2%