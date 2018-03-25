git add .
git commit -m 'Auto deploy'
git push
ssh 192.168.1.10 'cd ~/mirrorball && git pull && . build.sh'
ssh 192.168.1.11 'cd ~/mirrorball && git pull && . build.sh'
