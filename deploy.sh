git add .
git commit -m 'Auto deploy'
git push
buildcmd = bash --login -c 'cd ~/mirrorball && git pull && . build.sh';
echo studio...
ssh 192.168.1.10 "${buildcmd}"
echo telly...
ssh 192.168.1.11 "${buildcmd}"
