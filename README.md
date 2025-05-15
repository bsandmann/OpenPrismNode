# OpenPrismNode (OPN)

An open-source implementation of the PRISM node that follows the [DID-PRISM specification](https://github.com/input-output-hk/prism-did-method-spec/blob/main/w3c-spec/PRISM-method.md).

**[📚 Documentation](https://bsandmann.github.io/OpenPrismNode/)**

## About OpenPrismNode

The OpenPrismNode enables anyone to resolve, create, update and deactivate decentralized identifiers (DIDs) on the Cardano Blockchain without depending on the Hyperledger Identus Cloud agent. It serves as an independent, community-driven alternative to IOG's reference implementation—prioritizing transparency, resilience, and ease of operation.

## Core Features

- **Full DID lifecycle** – Create, update, deactivate, and resolve DIDs
- **Dual sync engines** – Choose between Cardano DbSync or lightweight Blockfrost API
- **Compatible APIs** – REST and gRPC interfaces compatible with Identus Cloud agent
- **Universal Resolver & Registrar** – Conforms to W3C standards for interoperability
- **Multi-tenant model** – Host multiple organizations with separate wallet management
- **Docker-ready** – Easy deployment with pre-built container images

## Quick Links

- **Docker Image**: `docker pull ghcr.io/bsandmann/openprismnode:latest`
- **Hosted Instances**:
  - Preprod: [https://opn.preprod.blocktrust.dev](https://opn.preprod.blocktrust.dev) Free walletId for testing purposed ``beb041bbbc689c6762f7fb743735e9c39df25ad5``
  - Mainnet: [https://opn.mainnet.blocktrust.dev](https://opn.mainnet.blocktrust.dev)

## Getting Started

Set up OpenPrismNode using either:
- **[DbSync setup guide](https://bsandmann.github.io/OpenPrismNode/Guide_DbSync)** - For robust integration with your own Cardano node
- **[Blockfrost API setup guide](https://bsandmann.github.io/OpenPrismNode/Guide_blockfrost)** - For quick, lightweight deployment

## API Access

- **REST API**: Available at `/api/v1/` endpoints
  - [Preprod Swagger Docs](https://opn.preprod.blocktrust.dev/swagger/index.html)
  - [Mainnet Swagger Docs](https://opn.mainnet.blocktrust.dev/swagger/index.html)
- **gRPC**: Available for Identus integration (ports: 50054 preprod, 50053 mainnet)

## Development

The project was funded through Project Catalyst F11 under the title 'Open source PRISM Node' (1100214) and is maintained by the team at [blocktrust.dev](https://blocktrust.dev).

All planned features have been implemented:
- ✅ Parsing PRISM v2 operations from Cardano network
- ✅ Writing PRISM v2 operations
- ✅ API endpoints for all operations
- ✅ gRPC endpoints for agent communication
- ✅ Statistical information API
- ✅ Universal DID Resolver compatibility
- ✅ Universal DID Registrar compatibility
- ✅ Tenant-based system with wallet management
- ✅ Alternative sync via Blockfrost API

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on how to build the project and submit changes.

## License

Released under the Apache 2.0 license.
