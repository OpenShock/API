# Security Policy

OpenShock controls devices that people wear, so we take security reports seriously.
This file covers what is specific to the OpenShock backend (API, live control gateway and cron). Everything else, including scope, rules for
testing, response times, disclosure and safe harbor, is in the
**[OpenShock security policy](https://github.com/OpenShock/.github/blob/main/SECURITY.md)**.
Please read it before testing.

## Reporting a vulnerability

**Do not open a public issue, pull request or Discord thread for a security vulnerability.**

Report it privately through one of these channels:

- [GitHub private vulnerability reporting](https://github.com/OpenShock/API/security/advisories/new) (preferred)
- Email to [security@openshock.org](mailto:security@openshock.org)

We acknowledge reports within 3 working days (7 days at most), give a triage result within
14 days, and follow coordinated disclosure with a default deadline of 90 days. OpenShock does not
pay bounties; reporters are credited in the advisory and in our
[security acknowledgments](https://github.com/OpenShock/.github/blob/main/SECURITY-ACKNOWLEDGMENTS.md)
if they wish.

## Supported versions

| Version                                   | Receives security fixes |
| ----------------------------------------- | ----------------------- |
| Latest [release](https://github.com/OpenShock/API/releases) | Yes       |
| Older releases                            | No, update to the latest release |
| `develop` branch and pre-release images   | Handled as regular bugs unless a release is also affected |

The officially hosted instance at `api.openshock.app` always runs a current version.

## What we are most interested in

- Sending control commands to, pairing with, or reading data from hubs and shockers that are not
  yours and not shared with you.
- Bypassing share restrictions: intensity and duration limits, permissions, pause state, and
  public share links.
- Authentication and session flaws: login, password reset, email change, OAuth sign-in, cookies.
- API tokens acting beyond their granted permissions.
- Hub authentication towards the live control gateway, and anything that lets one hub
  impersonate another.
- The firmware update (OTA) instructions the backend sends to hubs.
- Access to another user's personal data, and privilege escalation to admin.
- Injection, remote code execution and server-side request forgery.
- Insecure defaults in the `docker-compose.yml` and configuration we ship.

## Testing

- Test against your own instance wherever possible. See
  [Deployment / Self Hosting](README.md#deployment--self-hosting).
- On the officially hosted service, use only accounts and devices you own, and **never send a
  command to another person's device**. No denial-of-service or high-volume automated testing.

## Not considered vulnerabilities in this repository

- Actions available to an instance administrator.
- Operational endpoints (`/metrics`, health checks) being reachable because a self-hoster widened
  the allowed networks or exposed the container directly.
- Issues that only arise from a self-hosted configuration that contradicts the documentation.
- Missing rate limits without a demonstrated security impact.

## For self-hosters

- Subscribe to security advisories: **Watch → Custom → Security alerts** on this repository.
- Publish your own security contact for your instance. See
  [security.txt](README.md#securitytxt) in the README. The contact details in this file are for
  OpenShock itself, not for your instance.
