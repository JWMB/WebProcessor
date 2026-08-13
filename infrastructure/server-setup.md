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
# WAIT, we moved to user admin_site!!
# rsync -Pavuz --exclude '**/bin/*' --exclude '**/obj/*' --exclude 'node_modules/*' --exclude '.svelte-kit/output/*' ~/source/repos/JWMB/WebProcessor/* ubuntu@safespring_sync2:/home/admin-site/source

# rsync -Pavuz ~/Desktop/WebProcessor_Files/training_export/* ubuntu@safespring_sync2:/home/ubuntu/exports
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


> sudo nano ProblemSource/TrainingApi/appsettings.Docker-secrets.json
> tail -f /var/log/nginx/access.log
> tail -f /var/log/nginx/error.log
> podman logs -tf source_app_1
> podman logs -tf source_api_1
> podman exec -ti source_api_1 /bin/bash
> podman stats source_api_1 --no-stream --format "table {{.NetInput}} {{.NetOutput}}"

https://news.ycombinator.com/item?id=38982805

systemctl --user enable --now podman-admin

sudo nano /etc/systemd/system/podman-admin.service
# hm, the suggested didn't work... /etc/systemd/user/podman-admin.service instead?
sudo systemctl daemon-reload
sudo systemctl enable podman-admin
sudo systemctl start podman-admin
#sudo systemd-analyze verify podman-admin.service
#systemd-analyze --global unit-paths
#sudo systemd-analyze verify /etc/systemd/user/podman-admin.service
```
[Unit]
Description=podman-compose admin (api, app, mongo)
After=network.target
# Description=%i rootless pod (podman-compose)

[Service]
Type=simple
# EnvironmentFile=%h/.config/containers/compose/projects/%i.env
#ExecStartPre=-/usr/bin/podman-compose up --no-start
#ExecStartPre=/usr/bin/podman pod start pod_%i
#ExecStart=/usr/bin/podman-compose wait
#ExecStop=/usr/bin/podman pod stop pod_%i
#ExecStart=/usr/bin/podman-compose -f /home/ubuntu/source/compose.yaml up
#ExecStop=/usr/bin/podman-compose -f /home/ubuntu/source/compose.yaml down
ExecStart=/usr/bin/podman-compose -f /home/admin-site/source/compose.yaml up
ExecStop=/usr/bin/podman-compose -f /home/admin-site/source/compose.yaml down
Restart=always
RestartSec=60
#User=ubuntu
User=admin-site

[Install]
#WantedBy=default.target
WantedBy=multi-user.target
```

# worked, but only when ubuntu user was logged in ("active login session")
# create a different user with lower permissions and try to use that instead

sudo adduser admin-site
sudo loginctl enable-linger admin-site
sudo mkdir /home/admin-site/source
sudo chmod o+x /home/admin-site/
sudo chown admin-site:ubuntu /home/admin-site/source
sudo chmod g+rwx /home/admin-site/source

# su admin-site
# su - admin-site
# If you need to switch users inside the terminal, always use a login shell flag (-l or -). This forces the system to completely wipe the previous user's environment variables and load the correct ones for your new user.

# No credentials matching localhost/trainingapi found in /home/admin-site/.docker/config.json
# The "No credentials matching" error means Podman cannot find a valid username and password (or token) in your configuration to authenticate with the registry you are trying to access

# maybe allow access to "ubuntu" user's registry? More elegant, but not sure how.
# for now, just rebuild images as admin-site user
# note: when using "USING <shortname>" we first need to pull image (e.g. podman pull docker.io/library/nginx:latest)


rsync -Pavuz --exclude '**/bin/*' --exclude '**/obj/*' --exclude 'node_modules/*' --exclude '.svelte-kit/output/*' ./* ubuntu@safespring_sync2:/home/admin-site/source

mongo and api seems to run fine, but app doesn't start. maybe error "exit code: 137" is related? No, that was on shutdown
logs have "podman start -a source_app_1" and a little later "exid code: 1"
Tried "podman compose up" in user "admin-site" but got "docker.errors.DockerException: Error while fetching server API version: ('Connection aborted.', FileNotFoundError(2, 'No such file or directory'))" 
using `podman logs source_app_1` - found error: "http" directive is not allowed here in /etc/nginx/conf.d/nginx-site.conf:13
only added for log format, not really needed - commented out section (and "dbg" reference)
But why did it work when running as ubuntu user?
Rebuild it with `podman build -t adminapp . -f Dockerfile.web` (as user admin-site)
Oh - make sure we have Azure LLM settings in secrets (and correct owner: sudo chown ubuntu:ubuntu /home/admin-site/source/ProblemSource/TrainingApi/appsettings.Docker-secrets.json)
Rebuild trainingapi
Restart service (as ubuntu)

ok, but now mongo data is lost after restart...

Inspect containers:
su - admin-site
podman ps --format json

podman volume ls
podman volume prune
podman system df -v

podman exec -ti source_mongo_1 /bin/bash
ls /etc/data 
...empty?
exit

podman volume inspect <id>
data in /home/admin-site/.local/share/containers/storage/volumes/...

The original volume still exists but 0 bytes..?
https://oneuptime.com/blog/post/2026-03-17-use-compose-volumes-podman/view

sudo mkdir /home/admin-site/.docker
sudo nano /home/admin-site/.docker/config.json
```
{
  "auths": {
    "://private-registry.com": {
      "auth": "BASE64_ENCODED_USER_AND_PASSWORD btoa('username:password')"
    }
  }
}
```