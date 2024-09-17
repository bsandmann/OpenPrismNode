# Instructions for generating the CardanoWalletApi client
The Client was generated using NSwag Studio.
The yaml for the wallet is available here: https://raw.githubusercontent.com/cardano-foundation/cardano-wallet/master/specifications/api/swagger.yaml

The problem is that the spec is using a feature of the OpenAPI spec, that allows the size reduction of the spec by
collapsing the common parts of the request and response objects into a single definition. This is not supported by NSwag.

I have written a simple tool to do the expansion: See here: https://github.com/bsandmann/YamlExpander
Build, publish and then run that tool with the newest version of the yaml file to generate an updated client.
