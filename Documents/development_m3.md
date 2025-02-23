# Development Report for Milestone 3 of the Open Source PRISM Node
## February 2025
The repository and project name is “OpenPrismNode” and can be found here: [OpenPrismNode](https://github.com/bsandmann/OpenPrismNode)

## Milestone 3
The development of the current milestone sadly had to be stopped for a few weeks due to personal reasons. By October 2024, most of the work items presented had already been completed as the project was nearly ready for on-time delivery before it had to be halted for a while. Luckily, this didn't affect the community so much, since the project was already ahead of the milestones in terms of features and had been deployed for community use. Work started again recently and should now complete the remaining milestones in fairly quick succession.

Development Focus
The primary goal for Milestone 3 was the ability to write signed PRISM operations to the blockchain. Previously, the focus was on reading operations, parsing, and validating them; now the focus is on accepting transactions (through API and gRPC) and writing them to the underlying Cardano blockchain (VDR). From the start, multiple approaches were possible, and an approach based on the Cardano-Wallet (a project from the Cardano Foundation) was chosen, as anticipated earlier. While the application allows receiving signed PRISM operations over HTTP, the main use case is receiving operations via gRPC, since this is how the Identus Cloud Agent communicates.

In practice, the Open PRISM Node (OPN) now serves as a more capable drop-in replacement for the older PRISM Node. This means that, due to a simple change in the docker-compose file of the Identus Cloud Agent, the Agent can now communicate with the OPN over gRPC and send commands to it. These commands are for reading on-chain data, but with M3, they can now also be used to write operations on-chain and get the current status of those operations.

Additionally, a DID-Universal resolver–compatible endpoint was implemented to provide DID resolution services for any application requiring PRISM DID resolution. This is a new feature that hasn't been provided before by any party and finally allows the usage of did:prism outside the walled garden of the Identus project.

With M3, a UI was also added to make the OPN accessible not only through the provided endpoints. The Blazor server–based UI has been built on top of the already tested API endpoints and calls them in the background. For authentication and authorization, the API key–based mechanism of the REST API was extended to allow claims-based identity roles in the UI. The user can now not only use the API with a single API key but also log in with that key in the UI and maintain a session.

To allow for multiple tenants, a shared-user account concept was created: the admin user can give out a key to anyone to log in to the semi-public UI of the OPN. This login allows for the creation of a custom wallet, which can then be funded and used to write operations on-chain. After creation, the UI provides the user with the recovery information, a funding address, and a key for subsequent logins to that specific wallet. Overall, this offers a practical and secure solution for handling tenants. For clarity, note that the security of DIDs written on-chain is unrelated to the security of the wallet address funding the write operations, as the cryptographic keys are different. Having multiple tenants and different parties writing on-chain is primarily for better tracking and usage limitations.

The last work item was to encapsulate the project into a Docker container, instead of requiring an installation of the .NET runtime and the provision of additional services. The project still requires access to dbsync and the Cardano Wallet, but it now comes fully packed with its own PostgreSQL database and a logging solution (Seq). This simplifies deployment significantly.

Writing and extending the tests was an ongoing process, but most of the testing actually happened on-chain. We went multiple times through the synchronization process, cross-checking the results with the output of the old PRISM node and ensuring readability of OPN-written operations with the Identus Cloud Agent. Since the OPN was deployed in a live environment in October, it produced a lot of logs. Reviewing these logs and investigating various cases in which the application crashed made it quite stable, as a few edge cases were identified.

### Current State
The application can connect to a partially or fully synced dbsync PostgreSQL database and fully sync with it.
This includes an optimized fast-sync mode for initial syncing, as well as a mode for continuous syncing.
Through an API and a UI, the application can be stopped and restarted at any point during this process.
It reads and processes all PRISM-related operations and stores them in its own PostgreSQL database.
It automatically handles forks and rollbacks.
It provides an HTTP and gRPC interface for all relevant PRISM operations (Create, Update, Deactivate, Resolve).
It offers a Universal-Resolver–compliant endpoint for DID resolution.
It offers a way to set up new wallets, sync them, and fund them using an API or the UI.
It is provided as a Docker container to simplify deployment.