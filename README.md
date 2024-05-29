# OpenPrismNode

The OpenPrismNode is an open source implementation of the PRISM node by IOG and follows the [DID-PRISM specification](https://github.com/input-output-hk/prism-did-method-spec/blob/main/w3c-spec/PRISM-method.md)

The project was funded through Project Catalyst F11 under the title 'Open source PRISM Node' (1100214).
The implementation work started with the beginning of F11 and should be finished by the end of 2024.

The OpenPrismNode will have the following features/characteristics as part of the roadmap:
- Dockerized applation written in .Net
- Able to parse PRISM v2 operations from the predprod/mainnet Cardano network (assumes the prior setup of Cardano node & dbSync)
- Able to write PRISM v2 operation on the supported networks
- API endpoints for all read and write opertions
- GRPC endpoints for PRISM agent communication
- API endpoints for a statistical information
- Endpoints to support the [Universal DID Resovler spec](https://w3c-ccg.github.io/did-resolution/)
- Endpoints to support the [Universal DID Registrar spec](https://identity.foundation/did-registration/)
- Tenant-based system, with login and seperate wallet management
- Web-based UI with statistics and current sync-state

Updates on the progress of development will be given in this repo, as well the use F11 Milestone-Page.
  
