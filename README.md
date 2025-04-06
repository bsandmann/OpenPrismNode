# OPN - OpenPrismNode
The OpenPrismNode (OPN) is an open source implementation of the PRISM node by IOG and follows the [DID-PRISM specification](https://github.com/input-output-hk/prism-did-method-spec/blob/main/w3c-spec/PRISM-method.md)

The project is currently under development, but already in beta testing.
## User Interface
- A hosted version of the OpenPrismNode is available here:
     - [https://opn.preprod.blocktrust.dev](https://opn.preprod.blocktrust.dev)
     - [https://opn.mainnet.blocktrust.dev](https://opn.mainnet.blocktrust.dev)
- To login use these passwords/API-keys:
     - Preprod: `kfUpMnvUf32KLi73KLifhahfQ!`
     - Mainnet: `JopLmfjU34Jf!aslfuHJfpJuq28`
- Login in gives you access to a "shared User" Account. This account allows you to create your own wallet. After the creation of the wallet, a wallet-Id is generated for you. You can now logout and use this wallet-Id to login again and see your wallet. You can now fund it (a funding address will get generated after the wallet is fully synced). 
## API
- You can see the API here `https://opn.preprod.blocktrust.dev/swagger/index.html` and here `https://opn.mainnet.blocktrust.dev/swagger/index.html`
- For most actions, like re-synincing or detailed information you need an Admin key. For the public instances this is of course not provided here, but you can run your own OPN instance.
- Nonetheless you can use the API for resolving DIDs:  e.g.
     - [https://opn.preprod.blocktrust.dev/api/v1/identifiers/did:prism:76b...](https://opn.preprod.blocktrust.dev/api/v1/identifiers/did:prism:76b8001d5a87070834092793f5f9d4702ac24e25f6aea60f11382819551c492c)
     - [https://opn.mainnet.blocktrust.dev/api/v1/identifiers/did:prism:66a...](https://opn.mainnet.blocktrust.dev/api/v1/identifiers/did:prism:66a78fb42c8936f8fa5b695dfce39c0d5cc64fd3a25c6cb7884268ed43d9707c)
## gRPC
- To connect Identus to the node use this configuration inside the identus-docker compose file:     
     - Preprod
      `PRISM_NODE_HOST: opn.preprod.blocktrust.dev`
      `PRISM_NODE_PORT: 50054`
     - Mainnet
      `PRISM_NODE_HOST: opn.mainnet.blocktrust.dev`
      `PRISM_NODE_PORT: 50053`
- Note that the gRPC connection currently uses a preconfigured shared wallet (which is funded, at least for preprod), since Identus currently does not allow to provide an API-key (in our case the `wallet-Id`) via gRPC.

## Get the image
`docker pull ghcr.io/bsandmann/openprismnode:latest`
     

## Development
The project was funded through Project Catalyst F11 under the title 'Open source PRISM Node' (1100214).

The OpenPrismNode will have the following features/characteristics as part of the roadmap:
- Able to parse PRISM v2 operations from the predprod/mainnet Cardano network (assumes the prior setup of Cardano node & dbSync) (**DONE**)
- Able to write PRISM v2 operation on the supported networks (**DONE**)
- API endpoints for all read and write opertions (**DONE**)
- GRPC endpoints for PRISM agent communication (**DONE**)
- API endpoints for a statistical information (**IN PROGRESS**)
- Endpoints to support the [Universal DID Resolver spec](https://w3c-ccg.github.io/did-resolution/) (**DONE**)
- Endpoints to support the [Universal DID Registrar spec](https://identity.foundation/did-registration/) (**DONE**)
- Tenant-based system, with login and seperate wallet management (**DONE**)
- Web-based UI with statistics and current sync-state (**MOSTLY DONE**)
- Alternative sync method using blockfrost API (**DONE**)

Updates on the progress of development will be given in this repo, as well the use F11 Milestone-Page.
  

