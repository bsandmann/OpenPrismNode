# Architecture

The diagram below shows how **OpenPrismNode (OPN)** sits between the Cardano ledger and the applications that read or write DIDs.

```mermaid
graph TD
    %% Data sources
    subgraph "Data sources"
        CardanoNode[Cardano Node (local)]
        DbSync[(DbSync PostgreSQL)]
        CardanoNode --> DbSync
        Blockfrost[(Blockfrost API)]
    end

    %% Core node
    OPN[OpenPrismNode]

    DbSync -->|new blocks| OPN
    Blockfrost -->|new blocks| OPN

    %% Read clients
    subgraph "Read clients"
        Curl[cURL / scripts]
        UR[Universal Resolver]
        Identus[Identus Cloud Agent]
    end

    OPN -->|HTTP| Curl
    OPN -->|HTTP| UR
    OPN -->|gRPC| Identus

    %% Write path
    subgraph "Write path"
        Wallet[Cardano Wallet]
    end
    Wallet -->|sign & fund txs| OPN
````

---

## 1 Data Ingestion

| Mode                    | Setup                                                                                                                                          | Pros                                                                             | Cons                                                                                          |
| ----------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------- |
| **DbSync** (PostgreSQL) | Requires a local **Cardano node** *and* DbSync service. Both are installed and maintained by the operator (outside of the OPN Docker Compose). | ‑ Full control over data and rollback handling.<br>‑ Works offline / air‑gapped. | ‑ Heavier footprint (≈ 25 GB for testnet, 200 GB+ for mainnet).<br>‑ Longer initial sync.     |
| **Blockfrost API**      | Sign up for a free API key and point OPN to the endpoint. No additional infrastructure.                                                        | ‑ Five‑minute setup.<br>‑ Zero maintenance.                                      | ‑ Third‑party dependency.<br>‑ Theoretical risk that edge‑case blocks are missing or delayed. |

### How OPN consumes blocks

* **Streaming loop** – OPN polls the chosen backend and stores PRISM‑relevant transactions in its own database.
* **Switchable** – you can rebuild your node with the other backend at any time; the internal storage schema remains identical.

---

## 2 Endpoints Exposed by OPN

| Purpose                                     | Protocol               | Typical Client                     | Path or Port                |
| ------------------------------------------- | ---------------------- | ---------------------------------- | --------------------------- |
| **Resolve DIDs**                            | HTTP (Swagger/OpenAPI) | Curl, browsers, Universal Resolver | `/api/v1/identifiers/{did}` |
| **Resolve DIDs**                            | gRPC                   | Identus (SSI agent)                | `:50053` (default)          |
| **Write DIDs** (create, update, deactivate) | HTTP + JSON            | Wallet integrations, scripts       | `/api/v1/operations/*`      |

> **Tip:** If you are integrating **Identus Cloud Agent**, simply override the `PRISM_NODE_HOST`/`PORT` variables to point at your OPN instance instead of IOG’s reference node.

---

## 3 Writing Transactions

To publish a DID operation, OPN needs access to a **Cardano wallet**:

1. **Create or import** a wallet (Yoroi, Lace, CLI, …)
2. Set the wallet’s REST endpoint and port in `docker‑compose.yml` (or `AppSettings__CardanoWalletApiEndpoint*`).
3. Fund the wallet with ADA for fees. OPN signs, builds and submits UTxOs on your behalf.

*If you only care about **reading** DIDs, leave the wallet fields blank – OPN will run perfectly in read‑only mode.*

> A community issue ([https://github.com/bsandmann/OpenPrismNode/issues/123](https://github.com/bsandmann/OpenPrismNode/issues/123)) tracks the possibility of using **Blockfrost API** for submit‑and‑fee‑less write flows. This remains experimental and is **out of scope** for the current release.

---

## 4 Putting It All Together

Running OPN typically follows these steps:

1. **Choose a backend** → local DbSync **or** Blockfrost API.
2. **Spin up OPN** via Docker (see Setup chapter).
3. **(Optional) Configure a wallet** if you plan to write DIDs.
4. **Integrate clients**:

    * Resolve only → curl or Universal Resolver.
    * Full SSI agent → Identus pointing at `grpc://your‑opn:50053`.
5. Monitor logs or the built‑in UI to track sync status and operations.

With this architecture you retain sovereign control when you want it—and a lightweight path when you don’t.
