# OpenShock API

OpenShock backend.

### API Documentation 
https://api.shocklink.net/swagger/

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
