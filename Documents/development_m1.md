
# Development Report for Milestone 1 of the Open Source PRISM Node
### May 2024

The repository and project name is “OpenPrismNode” going forward. This might change in the future.

## Milestone 1 Development Focus
The development for the first milestone focused primarily on reading and processing PRISM metadata on-chain. This metadata undergoes multiple decoding steps:

- Parsing of the JSON metadata format used for PRISM
- Extracting the inner Proto-buf string
- Decoding the Proto-buf
- Unwrapping the Atala-Block, Atala-Object, and the four different kinds of operations
- Parsing the operations into C# models and validating the contents according to the validation rules from the PRISM-DID specification 

Writing follows a similar process but in reverse. It involves constructing a C# model representing the DID, then validating, constructing a Proto-buf object, and preparing the JSON content.

In addition to the described process, code was developed to read data from a test instance of a PostgreSQL database to test parsing code on existing PRISM transactions.

All developed code is provided with tests (either unit tests with mocks or integration tests).

For validation, additional code was written to handle secp256k1 elliptic curves to validate the signatures of the transactions.

## Future Objectives
The work will continue with two main objectives:

- Write code to traverse the complete chain and detect and fetch all PRISM-related operations
- Set up a database to store the parsed operations

The challenges will mostly revolve around small details: handling invalid operations, covering all scenarios that can occur with broken data, and managing old versions of PRISM transactions that are no longer valid under the current specification.