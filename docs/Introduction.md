# Introduction to OpenPrismNode (OPN)

The *OpenPrismNode* (**OPN**) is an open‑source implementation of the PRISM node that follows the [DID‑PRISM specification](https://github.com/input-output-hk/prism-did-method-spec). 
It enables anyone to resolve, create, update and deactivate **decentralised identifiers (DIDs)** on the Cardano Blockchain without depending the Hyperledger Identus Cloud agent.

The OPN was started before IOG’s reference node became publicly available, and it continues to serve as an **independent, community‑driven alternative**—prioritising transparency, resilience and ease of operation.

---

## Why OPN Exists

* **Complementary to the official node** – the reference PRISM node is now open‑sourced, but OPN offers a alternative codebase and alternative design choices strengthening the overall ecosystem by providing choice.
* **Open API** – OPN is built to be fully available as an open API, allowing developers to build on top of it without needing to run the Hyperledger Identus Cloud agent, but still being fully compatible with it.
* **Full transparency** – released under the Apache 2.0 licence so every line is auditable, forkable and extendable.
* **Alternative design choices** – OPN is built with a focus on not only run based on DbSync, but also allowing for a lightweight Blockfrost API sync engine. 

---

## Core Capabilities

* **Full DID lifecycle** – create, update, deactivate and resolve DIDs in strict conformity with the spec.
* **Dual sync engines** – choose between native Cardano **dbsync** or the lightweight **Blockfrost API** for syncing either the mainnet or pre-production network.
* **REST & gRPC APIs** – compatible with the Idenus Cloud agent, but offers a REST API for easy integration with other services.
* **Universal Resolver & Registrar endpoints** – bridging Cardano to the wider DID ecosystem.
* **Tenant model and built‑in wallet** – host several organisations on one node; each tenant controls its own keys and funding.
* **Docker‑first deployment** – images on GitHub Container Registry make spinning up a node trivial.

---

## Development & Stewardship

OPN is built and maintained by the team behind **[blocktrust.dev](https://blocktrust.dev)**. The project was initially funded through Project Catalyst (Fund 11) and is actively developed in the open. Contributions—whether code, documentation, testing or feedback—are warmly welcomed via GitHub.

---

## About This Documentation

This documentation set guides you through understanding, deploying and integrating OPN. The remaining chapters cover:

1. **[Architecture](Architecture.md)** – modular components and data flow.
2. **Setup Guides** – choose between [DbSync](Guide_DbSync.md) or [Blockfrost API](Guide_blockfrost.md) for initial sync.
3. **[Connection to the Identus Agent](IdentusAgent.md)** – configuring SSI agents over gRPC.
4. **[Sync Process](SyncProcess.md)** – understanding how data synchronization works.
5. **[Wallet Management](WalletManagement.md)** – managing wallet integration.
6. **[Universal Resolver](Resolver.md)** - resolving DIDs using the OPN.
7. **[Universal Registrar](Registrar.md)** - registering DIDs using the OPN.
7. **[API Reference](Api.md)** – detailed API documentation.
8. **[Troubleshooting](Troubleshooting.md)** – common pitfalls and diagnostic tips.
