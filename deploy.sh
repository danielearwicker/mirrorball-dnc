pushd MirrorBall.Server
dotnet publish -f netcoreapp1.1 -c Release
pushd bin/Release/netcoreapp1.1/publish
cp Mirror* /Volumes/danielearwicker/mirrorball/
cp wwwroot/* /Volumes/danielearwicker/mirrorball/wwwroot/
cp Mirror* /Volumes/danielearwicker-1/mirrorball/
cp wwwroot/* /Volumes/danielearwicker-1/mirrorball/wwwroot/
popd
popd

