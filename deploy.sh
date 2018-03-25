git add .
git commit -m 'Auto deploy'
git push
echo studio...
ssh 192.168.1.10 'cd ~/mirrorball && git pull && . build.sh'
echo telly...
ssh 192.168.1.11 'cd ~/mirrorball && git pull && . build.sh'
