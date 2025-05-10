---
title: Troubleshooting
layout: default
nav_order: 11        # lower numbers appear higher in the sidebar
---

# Troubleshooting

## Sync Problems
### Startup errors
While under normal conditions the OPN should start without any problems, even after longer times of inactivity, there are some cases where the OPN might not start correctly. This might potentiall include invalid states, beauce the node was stoped unexpectedly while within an database operation or rollback procedure. 
Here is it recommend to resync the node from scratch - either by deleting the ledger from the UI (and then restarting the node) or by deleting the postgres database (volume) and starting the node again.

### Cardano Hardforks while using DbSync
In the past the OPN stopped working because of major updates in the underlying Cardano node. This usually happens at times of hardforks. With a change in the Cardano node, often the dbsync also needs too be updated which sometimes implies a database change in the Postgres database used by dbsync. In some cases this does cause a breaking change in the tables used by the OPN. The database calls of the OPN might then not work anymore. This usually happens first of the preprod network and then later on the mainnet as the new versions roll out slowy. In case DbSync is used it might be an option to switch over to Blockfrost API for the time until an updatee of the OPN is available. While a resync should not be required, a partial rollback of e.g. the last epoch might help to get the node back into a working state e.g. if the database was failing within an operation.

## Resolving DIDs
No known issues.

## Writing DIDs
### Multiple requests in short sequence
The OPN is using the Cardano Wallet for writing the transactions on chain. In case multiple request are send to the node in short amount of time, the Cardano Wallet has to process them in sequence. Here the eUTXO model has some limiations as the metadata operation has to be paid for and the wallet has to wait until the transaction is confirmed. This might take some time and the OPN will not be able to process the next request until the previous one is confirmed. The OPN will retry the operation until the Cardano Wallet accepts a new operation or the timeout limit is reached. To mitigate the problem it is advised to try two things:
- For funding the wallet, send multiple transactions in a row to the OPN. This allows to the Cardano Wallet to process them in parallel.
- Send the requests to the OPN with a small delay in between. This allows the Cardano Wallet to process the transactions in sequence and give the OPN headroom for retrying the operation.