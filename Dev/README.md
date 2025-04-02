# Development Setup

## Requirements
- Docker or Podman with Compose
- NET 9.0 SDK
- git (+ git bash if on Windows)

## Recommendations
- Jetbrains Rider

## Setup

Open a shell in the `dev` directory. (Git Bash on Windows)

### Postgres, Dragonfly (Redis Compatible Cache), WebUI 

Run the following command to start the local development databases.

```bash
docker-compose up -d
```

This starts Postgres and Dragonfly as a container on your local machine.
Additionally, it starts the OpenShock WebUI in a container for easier access to the localhost backend.
Its accessible at `http://localhost:8080`.

There shouldn't be any errors in the output.

**Tips:**
- You can use `docker ps` to check if the containers are running.
- To update the images you need to run `docker-compose pull` and then `docker-compose up -d` again.
- To stop the containers, run `docker-compose down`.

### Setting up environment secrets

Make sure you are in the `dev` directory and your terminal is a linux like bash terminal (Git Bash on Windows will work).

Run the `setupUsersecrets.sh` script to setup dotnet user secrets for the projects.
```bash
./setupUsersecrets.sh
```

It will prompt you for your local machines ipv4 address. You can find this by running `ipconfig` on Windows or `ifconfig` on Mac/Linux.
We need this to be able to connect hubs to this locally running openshock instance.

### Running API

If not already done, open the OpenShockBackend solution in Rider.
Give it some time to index and restore nuget.

When everything is done you should be able to select the `API` launch config at the top right and click the green play button to start the API.
It's important to do this to run the initial migrations against the database.

### Seeding Test Data

Run the `setupTestData.sh` script to create a test user account.

```bash
./setupTestData.sh
```

The user has the following credentials:

Email: `test@openshock.org`
Username: `OpenShock-Test`
Password: `OpenShock123!`

PS: The locally started WebUI is available at `http://localhost:8080`.


### Running the other projects

### Connecting a Hub to this locally running instance

### Creating migrations 