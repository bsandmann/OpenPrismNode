
# Development Report for Milestone 2 of the Open Source PRISM Node
### August 2024

The repository and project name is “OpenPrismNode” and can be found here: [OpenPrismNode](https://github.com/bsandmann/OpenPrismNode)

## Milestone 2 Development Focus

The development focus for the second milestone was primarily on implementing an application capable of scanning the entire chain and extracting all PRISM-related operations. While the first milestone involved reading selected PRISM operations for proof-of-concept and demonstration purposes, the second milestone addressed the challenges of scanning and handling a real-world dataset with all its complexities and edge cases.

As expected, the real-world dataset was not as clean as the test data, necessitating revisions to some initial assumptions. This was anticipated as part of the development process, especially since the pre-production network is used for testing and contains a non-trivial amount of broken or outdated data.

Moreover, the task of developing a robust application capable of handling various issues—from database unavailability to unexpected data—became evident. Necessary performance improvements and a CHANG-hardfork in between also contributed to some delays.

### Current State

- The application can connect to a partially or fully synced `dbsync` PostgreSQL database and fully sync with it.
- This includes an optimized fast-sync mode for initial syncing, as well as a mode for continuous syncing.
- Through an API, the application can be stopped and restarted at any point during this process.
- It reads and processes all PRISM-related operations and stores them in its own PostgreSQL database. This includes `CreateDid`, `UpdateDid`, `DeactivateDid`, and `ProtocolVersionUpdate` operations.
- It processes all variants of new and old key formats, including `secp256k1`, `ED25519`, and `X25519`.
- It reads all types of service operations and specifically handles all variants of the endpoint format.
- For validation, it applies all the rules defined in the DID-PRISM Specification but also handles some edge cases more stringently than the specification requires. For example, keys must be added before they can be removed, and the same rule applies to services. The current specification allows more flexibility, even in scenarios where potentially nonsensical data could be included in an operation.
- Forks and rollbacks are managed by the application as follows:
  - Forks are defined as alternative branches in the chain whose tip heights are lower than or equal to the current tip.
  - If a fork is detected, all PRISM operations on transactions in the forked blocks will be rolled back.
  - The new valid branch will then be rescanned, and processing will continue once the process is completed.
  - This ensures that, at any point, only DIDs valid according to the latest information from the node are returned.
- The resolver also follows the specification and returns the latest valid DID state. The current format is not aligned with the W3C specification, but is still an internal representation. This will be addressed in the next milestone.

For the vast majority of operations, tests have been written to handle specific edge cases, primarily focusing on the ordering of update operations and different rollback scenarios. For a screenshot of the current test output, see [here](https://github.com/bsandmann/OpenPrismNode/blob/master/Documents/Testoutput_August_31_2024.jpg).

## Future Objectives

Work will continue with the following objectives:

- Develop code to write operations to the chain.
- Implement APIs and a web interface to interact with the node, including funding the node.
- Implement multi-tenant support.
- Enable the gRPC interface for the node.
