#!/bin/bash
pushd MirrorBall.Server
echo Building...
dotnet publish -f netcoreapp2.0 -c Release
popd
launchctl stop com.earwicker.mirrorball
