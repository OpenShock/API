# ShockLink API
ShockLink backend.

# Configuration

The API can be configured using the following environment variables:

| Variable                              | Default value           | Example value                                                                                |
|---------------------------------------|-------------------------|----------------------------------------------------------------------------------------------|
| `SHOCKLINK__DB`                       |                         | `Host=docker-node;Port=1337;Database=root;Username=root;Password=root;Search Path=ShockLink` |
| `SHOCKLINK__FRONTENDBASEURL`          | `https://shocklink.net` | `https://shocklink.net`                                                                      |
| `SHOCKLINK__REDIS__HOST`              | x                       | `redis`                                                                                      |
| `SHOCKLINK__REDIS__PORT`              | `6379`                  |
| `SHOCKLINK__REDIS__USER`              |                         |
| `SHOCKLINK__REDIS__PASSWORD`          |                         |
| `SHOCKLINK__CLOUDFLARE__ACCOUNTID`    |                         |
| `SHOCKLINK__CLOUDFLARE___IMAGES__KEY` |                         |
| `SHOCKLINK__CLOUDFLARE___IMAGES__URL` |                         |
| `SHOCKLINK__MAILJET__KEY`             |                         |
| `SHOCKLINK__MAILJET__SECRET`          |                         |

# Deployment

The ShockLink stack consists of the following components:

- Postgres as database
- Redis
- The API (this repository)
- [The WebUI](https://github.com/Shock-Link/WebUI)

## Using Docker

Assuming you have all other required containers running (if not, see above), you can run the following command to start
the API:

```bash
docker run \
    ghcr.io/shock-link/api:latest \
    --name shocklink-api \
    -e SHOCKLINK__DB=Host=docker-node;Port=1337;Database=root;Username=root;Password=root;Search Path=ShockLink \
    -e SHOCKLINK__REDIS__HOST=localhost \
    -e SHOCKLINK__FRONTENDBASEURL=https://myshocklink.app \
    -p 80:80/tcp
```

## Using `docker-compose`

See [docker-compose.yml](docker-compose.yml).

# Current struggles
+ Dependency to Cloudflare Images (Paid)
+ Dependency with Mailjet templates