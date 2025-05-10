---
title: API Reference
layout: default
nav_order: 10        # lower numbers appear higher in the sidebar
---

# OpenPrismNode API Documentation

This document provides a comprehensive reference for the OpenPrismNode REST API endpoints, organized by controller. Each section includes available endpoints, request/response formats, and example usages.

## Table of Contents

- [Ledgers Controller](#ledgers-controller)
- [Operations Controller](#operations-controller)
- [Statistics Controller](#statistics-controller)
- [Sync Controller](#sync-controller)
- [Wallets Controller](#wallets-controller)

## Authentication

Most endpoints require authentication using one of the following authorization roles:
- `ApiKeyOrAdminRoleAuthorization` - Requires admin privileges
- `ApiKeyOrUserRoleAuthorization` - Standard user access
- `ApiKeyOrWalletUserRoleAuthorization` - Specific for wallet operations

Authentication is provided via the `Authorization` header.

---

## Ledgers Controller

Manages ledger data including deletion of ledgers, blocks, and epochs.

### Endpoints

#### DELETE `/api/v1/ledgers/{ledger}`

Deletes the complete ledger for a specified network.

**Parameters:**
- `ledger` - The ledger to delete: 'preprod', 'mainnet', or 'inmemory'

**Authorization:** Admin role required

**Example:**
```http
DELETE http://localhost:5001/api/v1/ledgers/preprod
Accept: application/json
Content-Type: application/json
Authorization: secretPhrase
```

**Responses:**
- `200 OK` - Ledger successfully deleted
- `400 Bad Request` - Invalid ledger value
- `401 Unauthorized` - Authentication failed

**Notes:**
- This operation disables automatic syncing during deletion
- The sync service needs manual restart after deletion

---

#### DELETE `/api/v1/ledgers/{ledger}/block`

Deletes blocks from the tip down to a specified block height.

**Parameters:**
- `ledger` - The ledger to delete blocks from: 'preprod', 'mainnet', or 'inmemory'
- `blockHeight` (optional, query parameter) - The block height to delete up to (not included)

**Authorization:** Admin role required

**Examples:**

Delete only the tip block:
```http
DELETE http://localhost:5001/api/v1/ledgers/preprod/block
Accept: application/json
Content-Type: application/json
Authorization: secretPhrase
```

Delete blocks down to a specific height:
```http
DELETE http://localhost:5001/api/v1/ledgers/preprod/block?blockHeight=1000
Accept: application/json
Content-Type: application/json
Authorization: secretPhrase
```

**Responses:**
- `200 OK` - Blocks successfully deleted
- `400 Bad Request` - Invalid parameters
- `401 Unauthorized` - Authentication failed

**Notes:**
- If no block height specified, only the tip block is deleted
- Not recommended for deleting large numbers of blocks

---

#### DELETE `/api/v1/ledgers/{ledger}/epochs`

Deletes epochs from the tip down to a specified epoch number.

**Parameters:**
- `ledger` - The ledger to delete epochs from: 'preprod', 'mainnet', or 'inmemory'
- `epochNumber` (optional, query parameter) - The epoch number to delete up to (not included)

**Authorization:** Admin role required

**Examples:**

Delete only the most recent epoch:
```http
DELETE http://localhost:5001/api/v1/ledgers/preprod/epochs
Accept: application/json
Content-Type: application/json
Authorization: secretPhrase
```

Delete epochs down to a specific number:
```http
DELETE http://localhost:5001/api/v1/ledgers/preprod/epochs?epochNumber=10
Accept: application/json
Content-Type: application/json
Authorization: secretPhrase
```

**Responses:**
- `200 OK` - Epochs successfully deleted
- `400 Bad Request` - Invalid parameters
- `401 Unauthorized` - Authentication failed

**Notes:**
- Also cleans up orphaned addresses after epoch deletion
- Automatic syncing is disabled during operation

---

## Operations Controller

Manages operation status tracking.

### Endpoints

#### GET `/api/v1/operations/{operationStatusIdHex}`

Gets the status of an operation by its ID.

**Parameters:**
- `operationStatusIdHex` - Hexadecimal operation ID

**Authorization:** User role required

**Example:**
```http
GET http://localhost:5001/api/v1/operations/7c818138c5f66185eb80f16178ed7d8c5de1b52bd8443cff0aa1ffedd7b618cb
Accept: application/json
Content-Type: application/json
Authorization: secretPhraseUser
```

**Response:**
```json
{
  "operationId": "7c818138c5f66185eb80f16178ed7d8c5de1b52bd8443cff0aa1ffedd7b618cb",
  "operationHash": "a1b2c3d4e5f6...",
  "status": "Confirmed",
  "createdUtc": "2024-05-01T12:00:00Z",
  "lastUpdatedUtc": "2024-05-01T12:05:30Z"
}
```

**Responses:**
- `200 OK` - Returns operation status
- `400 Bad Request` - Invalid operation ID
- `401 Unauthorized` - Authentication failed

---

## Statistics Controller

Provides statistical information about the ledger.

### Endpoints

#### GET `/api/v1/statistics/{ledger}/stakeaddresses/{day}`

Retrieves stake addresses active on a specific day.

**Parameters:**
- `ledger` - The ledger to query: 'preprod', 'mainnet', or 'inmemory'
- `day` - Date in YYYY-MM-DD format

**Authorization:** User role required

**Example:**
```http
GET http://localhost:5001/api/v1/statistics/preprod/stakeaddresses/2024-10-01
Accept: application/json
Content-Type: application/json
Authorization: secretPhrase
```

**Responses:**
- `200 OK` - Returns list of stake addresses
- `400 Bad Request` - Invalid parameters
- `401 Unauthorized` - Authentication failed

---

#### GET `/api/v1/statistics/{ledger}/dids`

Retrieves a list of all DIDs in the specified ledger.

**Parameters:**
- `ledger` - The ledger to query: 'preprod', 'mainnet', or 'inmemory'

**Authorization:** None required

**Example:**
```http
GET http://localhost:5001/api/v1/statistics/preprod/dids
```

**Responses:**
- `200 OK` - Returns list of DIDs
- `400 Bad Request` - Invalid ledger

---

## Sync Controller

Controls the synchronization service between blockchain and OpenPrismNode.

### Endpoints

#### POST `/api/v1/sync/stop`

Forces the automatic sync service to stop.

**Authorization:** Admin role required

**Example:**
```http
POST http://localhost:5001/api/v1/sync/stop
Accept: application/json
Content-Type: application/json
Authorization: secretPhrase
```

**Responses:**
- `200 OK` - Sync service successfully stopped
- `401 Unauthorized` - Authentication failed

**Notes:**
- The service will be paused until manually restarted

---

#### POST `/api/v1/sync/start`

Forces the automatic sync service to restart.

**Authorization:** Admin role required

**Example:**
```http
POST http://localhost:5001/api/v1/sync/start
Accept: application/json
Content-Type: application/json
Authorization: secretPhrase
```

**Responses:**
- `200 OK` - Sync service successfully restarted
- `401 Unauthorized` - Authentication failed

---

#### GET `/api/v1/sync/status`

Gets the current status of the sync service.

**Authorization:** Admin role required

**Example:**
```http
GET http://localhost:5001/api/v1/sync/status
Accept: application/json
Authorization: secretPhrase
```

**Response:**
```json
{
  "isRunning": true,
  "isLocked": false
}
```

**Responses:**
- `200 OK` - Returns sync status
- `401 Unauthorized` - Authentication failed

---

#### GET `/api/v1/sync/progress/{ledger}`

Gets the sync progress of a specific ledger.

**Parameters:**
- `ledger` - The ledger to query: 'preprod', 'mainnet', or 'inmemory'

**Authorization:** User role required

**Example:**
```http
GET http://localhost:5001/api/v1/sync/progress/preprod
Accept: application/json
Authorization: secretPhrase
```

**Response:**
```json
{
  "isInSync": false,
  "blockHeightDbSync": 12345678,
  "blockHeightOpn": 12345600
}
```

**Responses:**
- `200 OK` - Returns sync progress
- `400 Bad Request` - Invalid ledger
- `401 Unauthorized` - Authentication failed

---

## Wallets Controller

Manages Cardano wallets for DID operations.

### Endpoints

#### POST `/api/v1/wallets`

Creates a new wallet.

**Authorization:** User role required

**Request Body:**
```json
{
  "name": "MyTestWallet"
}
```

**Example:**
```http
POST http://localhost:5001/api/v1/wallets
Accept: application/json
Content-Type: application/json
Authorization: secretPhrase

{
  "name": "MyTestWallet"
}
```

**Response:**
```json
{
  "mnemonic": "chicken grant north tower sell knock grab scrap burden floor advance...",
  "walletId": "f2f4c751c9fba3283c63f5e1dd1bb937021ceee2"
}
```

**Responses:**
- `200 OK` - Wallet created successfully
- `400 Bad Request` - Invalid request or CardanoWalletApiEndpoint not configured
- `401 Unauthorized` - Authentication failed

**Notes:**
- Store the mnemonic securely; it cannot be retrieved later

---

#### POST `/api/v1/wallets/restore`

Restores a wallet from mnemonic.

**Authorization:** User role required

**Request Body:**
```json
{
  "mnemonic": "chicken grant north tower sell knock grab scrap burden floor advance...",
  "name": "MyRestoredWallet"
}
```

**Example:**
```http
POST http://localhost:5001/api/v1/wallets/restore
Accept: application/json
Content-Type: application/json
Authorization: secretPhrase

{
  "mnemonic": "chicken grant north tower sell knock grab scrap burden floor advance...",
  "name": "MyRestoredWallet"
}
```

**Response:**
```json
{
  "walletId": "f2f4c751c9fba3283c63f5e1dd1bb937021ceee2"
}
```

**Responses:**
- `200 OK` - Wallet restored successfully
- `400 Bad Request` - Invalid mnemonic or CardanoWalletApiEndpoint not configured
- `401 Unauthorized` - Authentication failed

---

#### GET `/api/v1/wallets/{walletId}`

Gets details for a specific wallet.

**Parameters:**
- `walletId` - The ID of the wallet to retrieve

**Authorization:** Wallet user role required

**Example:**
```http
GET http://localhost:5001/api/v1/wallets/f2f4c751c9fba3283c63f5e1dd1bb937021ceee2
Accept: application/json
Content-Type: application/json
Authorization: secretPhrase
```

**Response:**
```json
{
  "walletId": "f2f4c751c9fba3283c63f5e1dd1bb937021ceee2",
  "balance": 5000000,
  "syncingComplete": true,
  "syncProgress": 100,
  "fundingAddress": "addr1q8z5hy6jdtj8qr..."
}
```

**Responses:**
- `200 OK` - Returns wallet details
- `400 Bad Request` - Invalid wallet ID or CardanoWalletApiEndpoint not configured
- `401 Unauthorized` - Authentication failed

---

#### GET `/api/v1/wallets/`

Gets all wallets (admin only).

**Authorization:** Admin role required

**Example:**
```http
GET http://localhost:5001/api/v1/wallets/
Accept: application/json
Content-Type: application/json
Authorization: secretPhrase
```

**Response:**
```json
[
  {
    "walletId": "f2f4c751c9fba3283c63f5e1dd1bb937021ceee2",
    "balance": 5000000,
    "syncingComplete": true,
    "syncProgress": 100,
    "fundingAddress": "addr1q8z5hy6jdtj8qr..."
  },
  {
    "walletId": "a1b2c3d4e5f6...",
    "balance": 2500000,
    "syncingComplete": true,
    "syncProgress": 100,
    "fundingAddress": "addr1q8z5hy6jdtj8qr..."
  }
]
```

**Responses:**
- `200 OK` - Returns list of wallets
- `400 Bad Request` - CardanoWalletApiEndpoint not configured
- `401 Unauthorized` - Authentication failed

---

#### POST `/api/v1/wallets/{walletId}/transactions`

Executes a transaction against a wallet.

**Parameters:**
- `walletId` - The ID of the wallet to use

**Request Body:**
- Base64-encoded `SignedAtalaOperation` protobuf

**Content-Type:** text/plain

**Authorization:** Wallet user role required

**Example:**
```http
POST http://localhost:5001/api/v1/wallets/f2f4c751c9fba3283c63f5e1dd1bb937021ceee2/transactions
Accept: application/json
Content-Type: text/plain
Authorization: secretPhrase

AQIDBAUGBwo=
```

**Response:**
- Hexadecimal operation status ID

**Responses:**
- `200 OK` - Transaction executed successfully
- `400 Bad Request` - Invalid input or CardanoWalletApiEndpoint not configured
- `401 Unauthorized` - Authentication failed

**Notes:**
- Can accept either Base64-encoded or JSON-formatted `SignedAtalaOperation`

---

#### GET `/api/v1/wallets/{walletId}/transactions`

Gets all transactions for a wallet.

**Parameters:**
- `walletId` - The ID of the wallet

**Authorization:** Wallet user role required

**Example:**
```http
GET http://localhost:5001/api/v1/wallets/f2f4c751c9fba3283c63f5e1dd1bb937021ceee2/transactions
Accept: application/json
Authorization: secretPhrase
```

**Response:**
```json
[
  {
    "transactionId": "tx123...",
    "fee": 170000,
    "operationStatusId": "96ea4587a96f408e08fa253f354650d34e6331eda4ea02b2e12e9d3f3ee3db5b",
    "operationHash": "a1b2c3d4e5f6...",
    "operationType": "CreateDid",
    "status": "Confirmed"
  },
  {
    "transactionId": "tx456...",
    "fee": 150000,
    "operationStatusId": "7c818138c5f66185eb80f16178ed7d8c5de1b52bd8443cff0aa1ffedd7b618cb",
    "operationHash": "d4e5f6a1b2c3...",
    "operationType": "UpdateDid",
    "status": "Pending"
  }
]
```

**Responses:**
- `200 OK` - Returns list of transactions
- `400 Bad Request` - Invalid wallet ID or CardanoWalletApiEndpoint not configured
- `401 Unauthorized` - Authentication failed

---

#### POST `/api/v1/wallets/{walletId}/withdrawal/{withdrawalAddress}`

Withdraws funds from a wallet to a specified address.

**Parameters:**
- `walletId` - The ID of the wallet
- `withdrawalAddress` - The Cardano address to withdraw to

**Authorization:** Wallet user role required

**Example:**
```http
POST http://localhost:5001/api/v1/wallets/f2f4c751c9fba3283c63f5e1dd1bb937021ceee2/withdrawal/addr1q8z5hy6jdtj8qr...
Accept: application/json
Content-Type: application/json
Authorization: secretPhrase

{}
```

**Responses:**
- `200 OK` - Withdrawal successful
- `400 Bad Request` - Invalid parameters or CardanoWalletApiEndpoint not configured
- `401 Unauthorized` - Authentication failed