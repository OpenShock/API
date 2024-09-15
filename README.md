# OpenShock API

OpenShock backend

### API Documentation 
You can access our Open API Doc here:

https://api.openshock.app/swagger

# Configuration

The API can be configured using the following environment variables:
Preferred way is a .env file.

| Variable                            | Required | Default value | Allowed / Example value                                                                                  |
|-------------------------------------|----------|---------------|----------------------------------------------------------------------------------------------------------|
| `OPENSHOCK__DB__CONN`               | x        |               | `Host=postgres-server-host;Port=5432;Database=openshock;Username=openshock;Password=superSecurePassword` |
| `OPENSHOCK__DB__SKIPMIGRATION`      |          | `false`       | `true`, `false`                                                                                          |
| `OPENSHOCK__DB__DEBUG`              |          | `false`       | `true`, `false`                                                                                          |
| `OPENSHOCK__FRONTEND__BASEURL`      | x        |               | `https://my-openshock-instance.net` or `https://shocklink.net`                                           |
| `OPENSHOCK__FRONTEND__SHORTURL`     | x        |               | `https://myoi.net` or `https://shockl.ink`                                                               |
| `OPENSHOCK__FRONTEND__COOKIEDOMAIN` | x        |               | `my-openshock-instance.net`                                                                              |
| `OPENSHOCK__REDIS__HOST`            | x        |               | `redis-server-host`                                                                                      |
| `OPENSHOCK__REDIS__PORT`            |          | `6379`        |                                                                                                          |   
| `OPENSHOCK__REDIS__USER`            |          |               |                                                                                                          |  
| `OPENSHOCK__REDIS__PASSWORD`        |          |               |                                                                                                          |  
| `OPENSHOCK__MAIL__SENDER__EMAIL`    | x        |               | `system@my-openshock-instance.net`                                                                       |
| `OPENSHOCK__MAIL__SENDER__NAME`     | x        |               | `MyOpenShockInstance System`                                                                             |
| `OPENSHOCK__MAIL__TYPE`             | x        |               | `MAILJET`, `SMTP`                                                                                        |
| `OPENSHOCK__TURNSTILE__ENABLE`      | x        |               | `true`, `false`                                                                                          |
| `OPENSHOCK__LCG__FQDN`              | x        |               | `de1-gateway.my-openshock-instance.net` `de1-gateway.shocklink.net`                                      |
| `OPENSHOCK__LCG__COUNTRYCODE`       | x        |               | `DE`                                                                                                     |

## Turnstile

When Turnstile enable is set to `true`, the following environment variable is required:

| Variable                          | Required | Default value | Allowed / Example value |
|-----------------------------------|----------|---------------|-------------------------|
| `OPENSHOCK__TURNSTILE__SITEKEY`   | x        |               |                         |
| `OPENSHOCK__TURNSTILE__SECRETKEY` | x        |               |                         |  

## EMail

### MAILJET

You need these environment variables to use [Mailjet](https://www.mailjet.com/):

| Variable                                            | Required | Default value | Allowed / Example value |
|-----------------------------------------------------|----------|---------------|-------------------------|
| `OPENSHOCK__MAIL__MAILJET__KEY`                     | x        |               |                         |
| `OPENSHOCK__MAIL__MAILJET__SECRET`                  | x        |               |                         |
| `OPENSHOCK__MAIL__MAILJET__TEMPLATE__PASSWORDRESET` | x        |               |                         |

### SMTP

You need these environment variables to use SMTP:

| Variable                                   | Required | Default value | Allowed / Example value            |
|--------------------------------------------|----------|---------------|------------------------------------|
| `OPENSHOCK__MAIL__SMTP__HOST`              | x        |               | `mail.my-openshock-instance.net`   |
| `OPENSHOCK__MAIL__SMTP__USERNAME`          | x        |               | `system@my-openshock-instance.net` |
| `OPENSHOCK__MAIL__SMTP__PASSWORD`          | x        |               | `superSecurePassword`              |
| `OPENSHOCK__MAIL__SMTP__ENABLESSL`         |          | `true`        | `true` or `false`                  |
| `OPENSHOCK__MAIL__SMTP__VERIFYCERTIFICATE` |          | `true`        | `true` or `false`                  |

# Deployment

The OpenShock stack consists of the following components:

- Postgres as database
- Redis-Stack (with keyspace events KEA)
- The API (this repository)
- [The WebUI](https://github.com/OpemShock/WebUI)

## Important

OpenShock instance needs to be under the same domain name to work correctly. This is due to cookie limitations in
browsers.
E.g.
Fontend: https://openshock.app
API: https://api.openshock.app
LCG: https://de1-gateway.openshock.app

## Using Docker

Grab the docker-compose.yml

Edit the enviroment files in the env folder

Change the postgres data storage location in docker-compose.yml from `/path/to/postgres-data` to the location you want

Run with `docker compose up -d`

the service needs https to work, setup a reverse proxy

- webui/frontend/share domain -> 5002
- api -> 5001
- lcg -> 5003

## Support development!

You can support the OpenShock Dev Team here: [Sponsor OpenShock](https://github.com/sponsors/OpenShock)
