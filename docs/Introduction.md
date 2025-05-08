# Introduction to OpenPrismNode (OPN)

*OpenPrismNode* (**OPN**) is an open‑source implementation of the PRISM node that follows the [DID‑PRISM specification](https://github.com/input-output-hk/prism-did-method-spec). It enables anyone to create, update, resolve and register **decentralised identifiers (DIDs)** on Cardano without depending on a single vendor‑controlled service.

OPN was started before IOG’s reference node became publicly available, and it continues to serve as an **independent, community‑driven alternative**—prioritising transparency, resilience and ease of operation.

---

## Why OPN Exists

* **Full transparency** – released under the Apache 2.0 licence so every line is auditable, forkable and extendable.
* **Operational diversity** – encouraging multiple independent operators (stake‑pool owners, enterprises, hobbyists) removes single‑point‑of‑failure risk.
* **Community‑led roadmap** – features are prioritised through open discussion and pull requests, ensuring the project evolves with real‑world needs.
* **Complementary to the official node** – the reference PRISM node is now open‑sourced, but OPN offers a lightweight codebase, alternative design choices and a separate governance model—strengthening the overall ecosystem by providing choice.

---

## Core Capabilities

* **Full DID lifecycle** – create, update, deactivate and resolve DIDs in strict conformity with the spec.
* **Multi‑network aware** – pre‑production *and* mainnet support out of the box.
* **Dual sync engines** – choose between native Cardano **dbsync** or the lightweight **Blockfrost API**.
* **REST & gRPC APIs** – uniform interfaces for wallets, mobile agents and backend services.
* **Universal Resolver & Registrar endpoints** – bridging Cardano to the wider DID ecosystem.
* **Tenant model and built‑in wallet** – host several organisations on one node; each tenant controls its own keys and funding.
* **Docker‑first deployment** – images on GitHub Container Registry make spinning up a node trivial.

---

## Development & Stewardship

OPN is built and maintained by the team behind **[blocktrust.dev](https://blocktrust.dev)**. The project was initially funded through Project Catalyst (Fund 11) and is actively developed in the open. Contributions—whether code, documentation, testing or feedback—are warmly welcomed via GitHub.

---

## Getting Involved

1. **Explore the code** – clone the repository, read the issues and open a discussion if you spot a bug or possible enhancement.
2. **Run a test node** – deploy locally or on a cloud VM and share performance or UX feedback.
3. **Join the conversation** – connect on Cardano Forum, SSI Discord channels or our weekly community calls.
4. **Submit pull requests** – from UI polish to protocol extensions, every contribution helps strengthen Cardano’s identity layer.

---

## About This Documentation

This documentation set guides you through understanding, deploying and integrating OPN. The remaining chapters cover:

1. **Architecture** – modular components and data flow.
2. **Prerequisites** – what you need before running the node.
3. **Setup Guides** – choose *dbsync* or *Blockfrost* for initial sync.
4. **Identus Integration** – configuring SSI agents over gRPC.
5. **Resolver & Registrar** – invoking the universal endpoints.
6. **Troubleshooting** – common pitfalls and diagnostic tips.

> **Next step:** Dive into **Architecture** to see how OPN fits together under the hood.
