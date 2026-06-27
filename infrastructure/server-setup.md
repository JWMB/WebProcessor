[HOME]\.ssh\config
Host safespring_sync2
    HostName 192.121.133.186
    IdentityFile ~/.ssh/safespring
    IdentitiesOnly yes
    User ubuntu
    Port 22


# get cert files from other server
# cd ~/source/repos/JWMB/WebProcessor/infrastructure
# rsync --dry-run -Pavuz ubuntu@safespring_sync2:/etc/letsencrypt/live/curricullm.org ./cert/
# --dry-run

```
cd ~/source/repos/JWMB/WebProcessor
rsync -Pavuz --exclude '**/bin/*' --exclude '**/obj/*' --exclude 'node_modules/*' --exclude '.svelte-kit/output/*' ./* ubuntu@safespring_sync2:/home/ubuntu/source
```
```
sudo apt-get update
sudo apt install nginx
# sudo apt install certbot python3-certbot-nginx -y
sudo apt-get remove certbot
sudo snap install --classic certbot
sudo ln -s /snap/bin/certbot /usr/local/bin/certbot
sudo certbot --nginx

sudo apt install podman
sudo apt install podman-compose
# sudo apt install docker-compose
mkdir source

# configure nginx site cd sites-enabled   sudo ln -s ../sites-available/foo.conf  sudo systemctl restart nginx
sudo nano /etc/nginx/sites-available/librelearn.conf
# configure
cd /etc/nginx/sites-enabled
sudo ln -s ../sites-available/librelearn.conf
sudo systemctl restart nginx

sudo certbot --nginx
# sudo certbot renew --dry-run

...
systemctl --user enable --now podman.socket
# systemctl --user status podman.socket

# podman build -t adminapp2 . -f Dockerfile.web2

cd source
podman build -t trainingapi . -f Dockerfile
podman build -t adminapp . -f Dockerfile.web
podman compose up -d
```