# Contributing to the OpenShock API

The general rules for contributing to OpenShock, including how pull requests are handled, are in the
[organization-wide contributing guide](https://github.com/OpenShock/.github/blob/main/CONTRIBUTING.md).
This file adds what is specific to this repository.

**Security vulnerabilities must not be reported in public issues or pull requests.** See [SECURITY.md](SECURITY.md).

## Getting started

- A guide for setting up a backend development environment is in the
  [Wiki](https://wiki.openshock.org/dev/contributing/backend), and local tooling is described in
  [Dev/README.md](Dev/README.md).
- The required .NET SDK version is pinned in `global.json`.
- The solution is `OpenShockBackend.slnx`.

## Before opening a pull request

- Branch from `develop` and target `develop` with your pull request.
- Build the solution and run the tests with `dotnet test`. The integration test projects start their dependencies
  in containers, so they need a running Docker daemon.
- Add or update tests for the behaviour you change.
- If you change the database model, include the EF Core migration.
- If you add or change a configuration variable, document it in [README.md](README.md).

Changes to authentication, authorization, device control, shares, API tokens or the gateway protocol are
safety-relevant. Say so in the pull request description and explain how you tested them.
