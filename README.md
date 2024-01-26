# OpenShock API

OpenShock backend.

# Configuration

The API can be configured using the following environment variables:

| Variable                           | Required | Default value | Example value                                                                                |
|------------------------------------|----------|---------------|----------------------------------------------------------------------------------------------|
| `OPENSHOCK__DB`                    | x        |               | `Host=docker-node;Port=1337;Database=root;Username=root;Password=root;Search Path=openshock` |
| `OPENSHOCK__FRONTENDBASEURL`       | x        |               | `https://shocklink.net`                                                                      |
| `OPENSHOCK__COOKIEDOMAIN`          | x        |               | `shocklink.net`                                                                              |
| `OPENSHOCK__REDIS__HOST`           | x        |               | `redis`                                                                                      |
| `OPENSHOCK__REDIS__PORT`           |          | `6379`        |                                                                                              |   
| `OPENSHOCK__REDIS__USER`           |          |               |                                                                                              |  
| `OPENSHOCK__REDIS__PASSWORD`       |          |               |                                                                                              |  
| `OPENSHOCK__MAIL__TYPE`            | x        |               | `MAILJET`, `SMTP`                                                                            |
| `OPENSHOCK__MAIL__MAILJET__KEY`    |          |               |                                                                                              |
| `OPENSHOCK__MAIL__MAILJET__SECRET` |          |               |                                                                                              |

# Deployment

The OpenShock stack consists of the following components:

- Postgres as database
- Redis
- The API (this repository)
- [The WebUI](https://github.com/Shock-Link/WebUI)

## Important

OpenShock instance needs to be under the same domain name to work correctly. This is due to cookie limitations in
browsers.
E.g.
Fontend: https://ShockLink.net
API: https://api.ShockLink.net
LCG: https://de1-gateway.shocklink.net

## Using Docker

See [docker-compose.yml](docker-compose.yml).

# Current struggles

+ Dependency with Mailjet templates
