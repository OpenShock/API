# ShockLink API


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
    ghcr.io/shock-link/shocklink-api:latest \
    --name shocklink-api \
    -e FRONTEND_BASE_URL=localhost \
    -e DB=localhost \
    -e REDIS_HOST=localhost \
    -p 80:80/tcp
```

## Using `docker-compose`

See [docker-compose.yml](docker-compose.yml).
