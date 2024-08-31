
# Development Report for Milestone 2 of the Open Source PRISM Node
### August 2024

The repository and project name is “OpenPrismNode” and can be found here: [OpenPrismNode](https://github.com/bsandmann/OpenPrismNode)

## Milestone 2 Development Focus
The development focus for second milestone was primarily the implementation of an application which was is able to scan the complete chain and extract all PRISM-related operations.
While the first milestone was reading selected PRISM operations for proof-of-concept and demonstration purposes, the second milestone was dealing with the realities of scanning and handling an real-world dataset, with all its challanges and edge cases.
As it turns out, the real-world dataset was not as clean as the test data, and some prior assumptions had to be revised. This was very much expected and part of the development process, since the preprod network is for testing a non trivial amount of data on it is broken our outdated.
Furthermore the realities of writing a robust application, which can deal with all kinds of issues from database unavailability to unexpected data, became apparent. Together with a set of nessaries performance improvement and a CHANG-hardfork in between caused some delays.

The current state:
- The application can be pointed a partially or fully synced dbsync postgres database will fully sync with it.
- This includes optimized fast-sync mode for initial syncing, as well as mode for continuous syncing.
- Through an API, the application can be stopped and restarted at any point in this process.
- It will read and process all PRISM-related operations, and store them its own postgres database. This includes CreateDid, UpdateDid, DeactivateDid and ProtocolVersionUpdate operations.
- It will process all variants for new and old key formates, including secp256k1,ED25519 and X25519.
- It will also read any kind of service operations and specifically all variants of the endpoint-format.
- For validation it will apply all the rules defined in the DID-PRISM-Specification, but will also handle some edge cases in a more stringent way, as defined in the spec. E.g. the keys must be added before they can be removed. The same it true for services. The current spec leaves more wiggle room here, for scenarios where potentially nonsensical data can be packed in an operation. 
- Forks and rollbacks are handled by the application in the following manner:
  - Forks are defined as alternative branches in the chain who's tip-height are lower or equal than the current tip
  - If a fork is detected all PRISM-operations on transactions on the forked blocks will be rolled back
  - The new valid branche will than be rescanned and processing continues if the process is completed
  - This ensures that at any point only DIDs will be returned that are valid to the newest information of the node

For the fast-majority of operations tests have been written to handle specifically certains edge cases, mostly arround the ordering of update-operations and different rollback senarios.
For a screenshot with the current output of tests see [here](https://github.com/bsandmann/OpenPrismNode/blob/master/Documents/Testoutput_August_31_2024.jpg)

## Future Objectives
The work will continue with the following objectives:

- Write code to write operations to chain
- Implementation of APIs and a web interface to interact with the node node, including for funding the node
- Implementation for multi-tenant-support
- Enabling the gRPC interface for the node
