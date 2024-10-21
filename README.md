# OPN - OpenPrismNode
The OpenPrismNode (OPN) is an open source implementation of the PRISM node by IOG and follows the [DID-PRISM specification](https://github.com/input-output-hk/prism-did-method-spec/blob/main/w3c-spec/PRISM-method.md)

The project is currently under development, but already in alpha testing.
## REST API
- A hosted version of the OpenPrismNode for API calls for **preprod** is available at https://opn.blocktrust.dev:31200 e.g
     `https://opn.blocktrust.dev:31200/api/v1/system/health` (Health check)
     `https://opn.blocktrust.dev:31200/api/v1/identifiers/did:prism:69ecb82551e3e08c092f14f720bae485a8808e5799e966ff1b415cf88d4107e2` (Resolver endpoint)
- For the creation of a new tenant (wallet), and writing of PRISM operations on chain an API-Key is required for preprod that is `authorization: kfUpMnvUf32KLi73KLifhahfQ!`.
- A full list of currently available endpoints can be found [here](https://github.com/bsandmann/O penPrismNode/blob/master/Documents/swagger.json)
- *While in development you might get an certificate error. Please set your application to ignore the certifcate for now. A fix will be delievered later*

## gRPC Endpoint
- An open gRPC endpoint is available for **preprod** at `opn.blocktrust.dev:31300` for the identus-agent
- Just replace the path and port variables in the docker-compose file with `opn.blocktrust.dev` and `31300` respectively.


## Mainnet
A seperate version of the OPN is already running on mainnet with the ability to write DIDs on chain. 
If you are interested in using the mainnet version, please contact us at [info@blocktrust.dev](mailto:info@blocktrust.dev)

## Development
The project was funded through Project Catalyst F11 under the title 'Open source PRISM Node' (1100214).
The implementation work started with the beginning of F11 and should be finished by the end of 2024.

The OpenPrismNode will have the following features/characteristics as part of the roadmap:
- Able to parse PRISM v2 operations from the predprod/mainnet Cardano network (assumes the prior setup of Cardano node & dbSync) (**DONE**)
- Able to write PRISM v2 operation on the supported networks (**DONE**)
- API endpoints for all read and write opertions (**DONE**)
- GRPC endpoints for PRISM agent communication (**DONE**)
- API endpoints for a statistical information (**IN PROGRESS**)
- Endpoints to support the [Universal DID Resolver spec](https://w3c-ccg.github.io/did-resolution/) (**DONE**)
- Endpoints to support the [Universal DID Registrar spec](https://identity.foundation/did-registration/) (**NOT STARTED**)
- Tenant-based system, with login and seperate wallet management (**IN PROGRESS**)
- Web-based UI with statistics and current sync-state (**NOT STARTED**)

Updates on the progress of development will be given in this repo, as well the use F11 Milestone-Page.
  

