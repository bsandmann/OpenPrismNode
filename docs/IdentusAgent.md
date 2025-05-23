---
title: Connecting Identus agent
layout: default
nav_order: 5        # lower numbers appear higher in the sidebar
---

# Identus Agent
## Context
The Identus Cloud Agent is a server-side self-sovereign-identity (SSI) service written in Scala that implements W3C DID standards, Aries workflows and DIDComm v2, exposing a REST API for creating/rotating Prism- or Peer-DIDs, issuing and verifying JWT, SD-JWT and AnonCreds credentials.

Applications integrate through ordinary HTTP requests and web-hook callbacks, letting the agent handle the cryptography, state machines and interoperability so developers can concentrate on business logic.
For on-chain operations the agent delegates to the offical IOG **PRISM node**.
Because the ledger interface is abstracted behind that node, you can repoint the agent to any Prism-compatible backend—such as your OpenPrismNode (OPN)—with no change to its other behaviour. The communication between the agent and the underlying node (e.g. OPN or Prism Node) is done via gRPC.
The specification of the the gRPC interface is available in the [PRISM spec](https://github.com/input-output-hk/prism-did-method-spec/blob/main/w3c-spec/PRISM-method.md) or even more condensed in the actual **proto files** which you can find in the [OPN repo](https://github.com/bsandmann/OpenPrismNode/tree/master/OpenPrismNode.Grpc/Protos)

## Using the Agent with OPN
The setup of the agent with the OPN as an alternative node is straight forward:
1. First locate the current **Docker compose file** of the agent. You can find it in the [Identus repo](https://github.com/hyperledger-identus/cloud-agent/blob/main/infrastructure/shared/docker-compose.yml).
2. Then you need to change the `PRISM_NODE_HOST` and `PRISM_NODE_PORT` environment variables to point to your OPN instance.
3. Finally you can now remove the `prism-node` service section from the docker compose file, as you do not need it anymore.
4. You can now start the agent with the OPN as a node by running the docker compose file as usual. Assume the OPN is already running the cloud agent will now call the OPN and communicate it for all the DID operations.

This is a minimal example of the docker compose file with the OPN as a node:
```yaml
---
version: "3.8"

services:
  ##########################
  # Database
  ##########################
  db:
    image: postgres:13
    environment:
      POSTGRES_MULTIPLE_DATABASES: "pollux,connect,agent,node_db"
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    volumes:
      - pg_data_db:/var/lib/postgresql/data
      - ./postgres/init-script.sh:/docker-entrypoint-initdb.d/init-script.sh
      - ./postgres/max_conns.sql:/docker-entrypoint-initdb.d/max_conns.sql
    ports:
      - "127.0.0.1:${PG_PORT:-5432}:5432"
    healthcheck:
      test: ["CMD", "pg_isready", "-U", "postgres", "-d", "agent"]
      interval: 10s
      timeout: 5s
      retries: 5

  pgadmin:
    image: dpage/pgadmin4
    environment:
      PGADMIN_DEFAULT_EMAIL: ${PGADMIN_DEFAULT_EMAIL:-pgadmin4@pgadmin.org}
      PGADMIN_DEFAULT_PASSWORD: ${PGADMIN_DEFAULT_PASSWORD:-admin}
      PGADMIN_CONFIG_SERVER_MODE: "False"
    volumes:
      - pgadmin:/var/lib/pgadmin
    ports:
      - "127.0.0.1:${PGADMIN_PORT:-5050}:80"
    depends_on:
      db:
        condition: service_healthy
    profiles:
      - debug

  ##########################
  # Services
  ##########################

  vault-server:
    image: hashicorp/vault:latest
    #    ports:
    #      - "8200:8200"
    environment:
      VAULT_ADDR: "http://0.0.0.0:8200"
      VAULT_DEV_ROOT_TOKEN_ID: ${VAULT_DEV_ROOT_TOKEN_ID}
    command: server -dev -dev-root-token-id=${VAULT_DEV_ROOT_TOKEN_ID}
    cap_add:
      - IPC_LOCK
    healthcheck:
      test: ["CMD", "vault", "status"]
      interval: 10s
      timeout: 5s
      retries: 5

  cloud-agent:
    image: docker.io/hyperledgeridentus/identus-cloud-agent:${AGENT_VERSION:-latest}
    environment:
      POLLUX_DB_HOST: db
      POLLUX_DB_PORT: 5432
      POLLUX_DB_NAME: pollux
      POLLUX_DB_USER: postgres
      POLLUX_DB_PASSWORD: postgres
      CONNECT_DB_HOST: db
      CONNECT_DB_PORT: 5432
      CONNECT_DB_NAME: connect
      CONNECT_DB_USER: postgres
      CONNECT_DB_PASSWORD: postgres
      AGENT_DB_HOST: db
      AGENT_DB_PORT: 5432
      AGENT_DB_NAME: agent
      AGENT_DB_USER: postgres
      AGENT_DB_PASSWORD: postgres
      POLLUX_STATUS_LIST_REGISTRY_PUBLIC_URL: http://${DOCKERHOST}:${PORT}/cloud-agent
      DIDCOMM_SERVICE_URL: http://${DOCKERHOST}:${PORT}/didcomm
      REST_SERVICE_URL: http://${DOCKERHOST}:${PORT}/cloud-agent
      PRISM_NODE_HOST: 1.2.3.4
      PRISM_NODE_PORT: 50053
      VAULT_ADDR: ${VAULT_ADDR:-http://vault-server:8200}
      VAULT_TOKEN: ${VAULT_DEV_ROOT_TOKEN_ID:-root}
      SECRET_STORAGE_BACKEND: postgres
      DEV_MODE: true
      DEFAULT_WALLET_ENABLED:
      DEFAULT_WALLET_SEED:
      DEFAULT_WALLET_WEBHOOK_URL:
      DEFAULT_WALLET_WEBHOOK_API_KEY:
      DEFAULT_WALLET_AUTH_API_KEY:
      GLOBAL_WEBHOOK_URL:
      GLOBAL_WEBHOOK_API_KEY:
      WEBHOOK_PARALLELISM:
      ADMIN_TOKEN:
      API_KEY_SALT:
      API_KEY_ENABLED: ${API_KEY_ENABLED:-true}
      API_KEY_AUTHENTICATE_AS_DEFAULT_USER: false
      API_KEY_AUTO_PROVISIONING:
    depends_on:
      db:
        condition: service_healthy
      vault-server:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://cloud-agent:8085/_system/health"]
      interval: 30s
      timeout: 10s
      retries: 5
    extra_hosts:
      - "host.docker.internal:host-gateway"

  swagger-ui:
    image: swaggerapi/swagger-ui:v5.1.0
    environment:
      - 'URLS=[
        { name: "Cloud Agent", url: "/docs/cloud-agent/api/docs.yaml" }
        ]'

  apisix:
    image: apache/apisix:2.15.0-alpine
    volumes:
      - ./apisix/conf/apisix.yaml:/usr/local/apisix/conf/apisix.yaml:ro
      - ./apisix/conf/config.yaml:/usr/local/apisix/conf/config.yaml:ro
    ports:
      - "${PORT}:9080/tcp"
    depends_on:
      - cloud-agent
      - swagger-ui

volumes:
  pg_data_db:
  pgadmin:
```

## Limitations
The Identus cloud agent can currently only use a single (default) wallet. Meaning a multitenant setup is not possible for writing DIDs, as there is currently no way of specifiying a ApiKey or a walletId in the request from the agent to the OPN. This is a limitation of the current implementation of the Identus cloud agent and hopefully will be fixed in the future. If multiple wallets are present in the OPN it automatically selects the wallet with the highest balance. In case a specific wallet is to be used it can be setup using the **DefaultWalletIdForGrpc** parameter.
See the [Configuration.md](Configuration.md) for more details.