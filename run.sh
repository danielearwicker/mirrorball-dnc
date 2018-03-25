#!/bin/bash
pushd MirrorBall.Server
dotnet restore
export ASPNETCORE_URLS=http://*:5000
dotnet watch run
