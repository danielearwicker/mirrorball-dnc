#!/bin/bash
pushd MirrorBall.Server
echo Building...
launchctl unload /Library/LaunchDaemons/com.earwicker.mirrorball.plist
dotnet publish -f netcoreapp2.0 -c Release
popd
pushd MirrorBall.Client
npm install
npx webpack
popd
launchctl load /Library/LaunchDaemons/com.earwicker.mirrorball.plist
