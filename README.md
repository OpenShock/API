# OpenShock API
OpenShock backend.

# Configuration

The API can be configured using the following environment variables:

| Variable                              | Default value           | Example value                                                                                |
|---------------------------------------|-------------------------|----------------------------------------------------------------------------------------------|
| `OPENSHOCK__DB`                       |                         | `Host=docker-node;Port=1337;Database=root;Username=root;Password=root;Search Path=openshock` |
| `OPENSHOCK__FRONTENDBASEURL`          | `https://shocklink.net` | `https://shocklink.net`                                                                      |
| `OPENSHOCK__REDIS__HOST`              | x                       | `redis`                                                                                      |
| `OPENSHOCK__REDIS__PORT`              | `6379`                  |
| `OPENSHOCK__REDIS__USER`              |                         |
| `OPENSHOCK__REDIS__PASSWORD`          |                         |
| `OPENSHOCK__MAILJET__KEY`             |                         |
| `OPENSHOCK__MAILJET__SECRET`          |                         |

# Deployment

The OpenShock stack consists of the following components:

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
    -e OPENSHOCK__DB=Host=docker-node;Port=1337;Database=root;Username=root;Password=root;Search Path=openshock \
    -e OPENSHOCK__REDIS__HOST=localhost \
    -e OPENSHOCK__FRONTENDBASEURL=https://myopenshock.app \
    -p 80:80/tcp
```

## Using `docker-compose`

See [docker-compose.yml](docker-compose.yml).

# Current struggles
+ Dependency with Mailjet templates
