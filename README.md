# ShockLink API

# Configuration

The API can be configured using the following environment variables:

|Variable|Example value|
|-|-|
|`DB`|`Host=docker-node;Port=1337;Database=root;Username=root;Password=root;Search Path=ShockLink`|
|`REDIS_HOST`|`redis`|
|`REDIS_PASSWORD`| |
|`CF_ACC_ID`| |
|`CF_IMG_KEY`| |
|`CF_IMG_URL`| |
|`MAILJET_KEY`| |
|`MAILJET_SECRET`| |

# Deployment

The ShockLink stack consists of the following components:
- Postgres as database
- Redis 
- The API (this repository)
- [The WebUI](https://github.com/Shock-Link/WebUI)

## Using Docker

Assuming you have all other required containers running (if not, see above), you can run the following command to start the API:

```bash
docker run \
    ghcr.io/shock-link/api:latest \
    --name shocklink-api \
    -e FRONTEND_BASE_URL=localhost \
    -e DB=localhost \
    -e REDIS_HOST=localhost \
    -p 80:80/tcp
```

## Using `docker-compose`

See [docker-compose.yml](docker-compose.yml).
