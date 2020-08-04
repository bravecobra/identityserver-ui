# Docker-compose

Make you also completes the other [prerequisites](../prerequisites/)

## Build the images

Now that everything resolves well, we have the certificates and we configured a Google OAuth client, we start up `docker-compose`. The building of the docker images is left in in this setup to make it easy enough to start this up.

```bash
docker-compose build
```

## Run the images

```bash
docker-compose up -d
```

## Test it out

Now point your browser to [https://sts.localhost.com](https://sts.localhost.com) to reach the IdentityServer itself and login. Or use one of the two preconfigured clients at [https://jsclient.localhost.com](https://jsclient.localhost.com) or [https://mvcclient.localhost.com](https://mvcclient.localhost.com).
