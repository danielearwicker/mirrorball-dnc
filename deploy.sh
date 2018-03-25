#!/bin/bash
git add .
git commit -m 'Auto deploy'
git push
buildcmd="bash --login -c 'cd ~/mirrorball && git pull && . build.sh'"

ssh 192.168.1.10 ${buildcmd} &
ssh 192.168.1.11 ${buildcmd} &
wait
