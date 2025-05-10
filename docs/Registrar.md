---
title: Universal Registrar API
layout: default
nav_order: 9        # lower numbers appear higher in the sidebar
---

# Registrar Overview

OpenPrismNode (OPN) implements a **Universal Registrar**-compatible API for the PRISM Decentralized Identifier (DID) method. This means OPN can create, update, and deactivate DIDs via a standardized interface, as described in the Decentralized Identity Foundation’s *DID Registration* draft specification. *Universal Registrar* is the lesser-known counterpart to the Universal Resolver: it handles **write operations** (DID creation and management) in a unified way across DID methods. The DID Registration spec is still a **working draft** with limited adoption, so support for it (especially for did\:prism) is cutting-edge and may evolve.
Note,that the Universal Registar API is not the only way to create DIDs on chain: Alternatively you can use the Identus Cloud agent and call the API of the agent to create, update or delete DIDs. The main difference between these methods is, that the Universal Registrar API does return all the secret keys, while the Cloud agent always retains them, binding them to the agent.

**OPN’s Registrar runs in Internal Secret Mode.** In this mode, the server generates and manages the cryptographic keys for the DID on behalf of the user. OPN will create the necessary keys internally and, by default, it **stores them and returns them to the client** in responses. This configuration (internal key management with `storeSecrets` and `returnSecrets` enabled) simplifies usage: clients do not need to provide their own keys or signatures for operations – OPN handles keys internally and can supply the private key material in responses if requested. (For production use, returning secrets may be disabled for security, but OPN’s default is to return them for convenience.)

> **Note:** Because OPN manages keys internally, performing updates or deactivations does not require the client to supply private keys for authentication. OPN will use the stored keys from the initial creation. Keep in mind that whoever operates the OPN service effectively controls the DID keys in this mode.

## Endpoints Overview

OPN’s Registrar API endpoints follow the structure of the Universal Registrar spec. All endpoints are under the base path `/api/v1/registrar/` and use **HTTP POST** for write operations. The DID method (in this case `prism`) is specified in the request. For OPN, since only the PRISM method is supported, the method can be indicated via a query parameter or in the request body as shown in examples.

The primary endpoints are:

* **Create a DID:** `POST /api/v1/registrar/create?method=prism` – Initiates creation of a new DID.
* **Update a DID:** `POST /api/v1/registrar/update?method=prism` – Submits an update operation (add, remove, or replace parts of the DID Document).
* **Deactivate a DID:** `POST /api/v1/registrar/deactivate?method=prism` – Initiates deactivation (revocation) of an existing DID.

All requests and responses use JSON. The request body generally includes an **`options`** object for method-specific parameters, a **`did`** (for update/deactivate), an **`operation`** specifier (for update), and a **`secret`** object (often empty `{}` in internal mode). Responses conform to the Universal Registrar output format, typically containing a **`jobId`** (if the operation is ongoing), a **`didState`** object with details of the DID’s state and keys, and metadata.

Below, we detail each operation with examples. The examples are taken from OPN’s API and show typical request bodies and responses.

## Creating a PRISM DID

To create a new PRISM DID, use the **create** endpoint. With the internal mode you do not need to provide any keys; OPN will generate an secp256k1 key pair (the default key type for did\:prism) and construct a DID Document with it containing at least the masterkey as a default key. 

**Request:** *Create a new DID* (did\:prism) with three keys and two service endpoints

```
POST {{OpenPrismNode_HostAddress}}/api/v1/registrar/create
Accept: application/json
Content-Type: application/json

{
  "method": "prism",
  "options": {
    "storeSecretes": true,
    "returnSecrets": true,
    "walletId": "a4e8d89055c08493b297883392e730008153b6c1",
    "network": "preprod"
  },
  "secret": {
    "verificationMethod": [
      {
        "id": "key-1",
        "type": "JsonWebKey2020",
        "purpose": [
          "authentication"
        ],
        "curve": "secp256k1"
      },
      {
        "id": "key-2",
        "type": "JsonWebKey2020",
        "purpose": [
          "authentication"
        ],
        "curve": "Ed25519"
      },
      {
        "id": "key-3",
        "type": "JsonWebKey2020",
        "purpose": [
          "keyAgreement"
        ],
        "curve": "X25519"
      }
    ]
  },
  "didDocument": {
    "@context": [
      "https://www.w3.org/ns/did/v1",
      "https://w3id.org/security/suites/jws-2020/v1",
      "https://identity.foundation/.well-known/did-configuration/v1",
      "https://didcomm.org/messaging/contexts/v2"
    ],
    "service": [
      {
        "id": "service-1",
        "type": "LinkedDomains",
        "serviceEndpoint": "https://opn.blocktrust.dev/"
      },
      {
        "id": "service-2",
        "type": "DIDCommMessaging",
        "serviceEndpoint": "https://opn.blocktrust.dev/"
      }
    ]
  }
}
```

In this example, we have specified the two imporant options:
- The network to use (in this case `preprod`) and
- The walletId refers to the wallet that is used to sign the transaction on chain, see [WalletManagement.md](WalletManagement.md) for more information.

**Initial Response:** 

```json
{
  "jobId": "8786c99ac02928b05ee502ad4bedc0cf177fe07247938222a8df4a98762b3c33",
  "didState": {
    "state": "wait",
    "wait": "Operation is pending or awaiting confirmation on the ledger."
  },
  "didRegistrationMetadata": {}
}
```

Let’s break down this response:

* **`jobId`**: The unique identifier for the create operation job. Because writing a PRISM DID involves submitting a transaction to the Cardano blockchain, it does not complete instantly. OPN returns a job ID so you can check on the status later. In this case, `b6e3da9d33dfb942f0243972d3a321baf670c26d37250c7c704e14aac6735e5c` identifies the pending creation.

* **`didState`**: Contains the current state of the DID operation.

    * `state: "wait"` indicates the process is still ongoing and the client should wait. No further action is needed from the client’s side.

At this point, the DID creation is **in progress**. The DID has been reserved and keys generated, but the DID Document is not yet published on the Cardano chain. You must **track the job** to know when it completes. This is normal behaivior for asynchronous operations in the Universal Registrar model.

## Checking DID Operation Status (Job ID)

After initiating a create operation, you use the **jobId** to check for completion. 

**Request:** *Check status of a DID operation job* (using the `jobId` from the create response). Note that this is again a **POST** request, as to the specification.

```
POST {{OpenPrismNode_HostAddress}}/api/v1/registrar/create
Accept: application/json
Content-Type: application/json

{
  "jobId": "8786c99ac02928b05ee502ad4bedc0cf177fe07247938222a8df4a98762b3c33"
}
```

**Response:** *Job completed (DID created)*

```json
{
  "jobId": "8786c99ac02928b05ee502ad4bedc0cf177fe07247938222a8df4a98762b3c33",
  "didState": {
    "state": "finished",
    "did": "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4",
    "secret": {
      "verificationMethod": [
        {
          "type": "JsonWebKey2020",
          "controller": "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4",
          "purpose": [
            "masterKey"
          ],
          "privateKeyJwk": {
            "crv": "secp256k1",
            "kty": "EC",
            "x": "-IBbEjQojMP8qf-6LoXuy0N1oxGrYiHugT1TA5bu8lY",
            "y": "II7i_LbpM83R5CTrV76UcIZbXGH59xZWAn-Ko9sgoYw",
            "d": "BD7nYhBadhJqQGi20LxKoMhDgJMLLoV7FjL7ySjktdI"
          }
        },
        {
          "id": "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4#key-1",
          "type": "JsonWebKey2020",
          "controller": "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4",
          "purpose": [
            "authentication"
          ],
          "privateKeyJwk": {
            "crv": "secp256k1",
            "kty": "EC",
            "x": "yeDHPB0q-7VgHPwbegDkGHPlWI7TGwsurHyNSlszgyU",
            "y": "2K98SwisxDUetas8B26oRKNGkkrqDUUwAlitRCMbliE",
            "d": "TZ5yZdXUxPvUu5fMOTSOdszW1suKNA2kekLtYKWRcqs"
          }
        },
        {
          "id": "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4#key-2",
          "type": "JsonWebKey2020",
          "controller": "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4",
          "purpose": [
            "authentication"
          ],
          "privateKeyJwk": {
            "crv": "Ed25519",
            "kty": "OKP",
            "x": "xx4YC2gBVXDlBe3vAZwf-bXC2LJ97oep1OpQv-ufZFg",
            "d": "5Y4HeFEZLd5XFj49NKInlIVtJVcCnuNE13fZIoWcwuo"
          }
        },
        {
          "id": "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4#key-3",
          "type": "JsonWebKey2020",
          "controller": "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4",
          "purpose": [
            "keyAgreement"
          ],
          "privateKeyJwk": {
            "crv": "X25519",
            "kty": "OKP",
            "x": "mGmZKmdgNyqk8YLj1F5UxyeKEKwrBOjHhNGHRCcOg3w",
            "d": "OH1w6Wp1MR_gdelkxc_lBr4HrcteVrncqllqwI9vGkk"
          }
        }
      ]
    },
    "didDocument": {
      "@context": [
        "https://www.w3.org/ns/did/v1",
        "https://w3id.org/security/suites/jws-2020/v1",
        "https://didcomm.org/messaging/contexts/v2",
        "https://identity.foundation/.well-known/did-configuration/v1"
      ],
      "id": "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4",
      "verificationMethod": [
        {
          "id": "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4#key-1",
          "type": "JsonWebKey2020",
          "controller": "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4",
          "publicKeyJwk": {
            "crv": "secp256k1",
            "kty": "EC",
            "x": "yeDHPB0q-7VgHPwbegDkGHPlWI7TGwsurHyNSlszgyU",
            "y": "2K98SwisxDUetas8B26oRKNGkkrqDUUwAlitRCMbliE"
          }
        },
        {
          "id": "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4#key-2",
          "type": "JsonWebKey2020",
          "controller": "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4",
          "publicKeyJwk": {
            "crv": "Ed25519",
            "kty": "OKP",
            "x": "xx4YC2gBVXDlBe3vAZwf-bXC2LJ97oep1OpQv-ufZFg"
          }
        },
        {
          "id": "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4#key-3",
          "type": "JsonWebKey2020",
          "controller": "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4",
          "publicKeyJwk": {
            "crv": "X25519",
            "kty": "OKP",
            "x": "mGmZKmdgNyqk8YLj1F5UxyeKEKwrBOjHhNGHRCcOg3w"
          }
        }
      ],
      "authentication": [
        "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4#key-1",
        "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4#key-2"
      ],
      "keyAgreement": [
        "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4#key-3"
      ],
      "service": [
        {
          "id": "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4#service-1",
          "type": "LinkedDomains",
          "serviceEndpoint": "https://opn.blocktrust.dev/"
        },
        {
          "id": "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4#service-2",
          "type": "DIDCommMessaging",
          "serviceEndpoint": "https://opn.blocktrust.dev/"
        }
      ]
    }
  },
  "didRegistrationMetadata": {
    "walletId": "a4e8d89055c08493b297883392e730008153b6c1",
    "mnemonic": "pulse accuse secret hockey hurt mountain stumble shield pen void reopen mutual barely bottom shallow air flash bleak essence cable poet lecture wage monster"
  },
  "didDocumentMetadata": {
    "canonicalId": "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4",
    "created": "2025-05-10T14:52:02Z",
    "versionId": "549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4",
    "cardanoTransactionPosition": 0,
    "operationPosition": 0,
    "originTxId": "99c781879e292378de12cb4d9b0af43a84157d6927c71d555f2177c1d5013a29"
  }
}
```

* `didState.state: "finished"` confirms the DID is fully registered.
* `didDocument` now contains the actual DID Document on-chain and is identical to the the same DID when just being resolved.
* The `secret` contains the same verification methods as in the diddocument section, but with two notable differences. 
  * The first verification method is the master key, which is not included in the did document. It is always using the secp256k1 crpypto algorithm and used to sign a new transaction on chain related to this DID. So when updating or deactivating the DID, this key is used to sign the transaction.
  * For each verification method the `privateKeyJwk` is included, which contains the private key material (under **d**). This is returned due to the `returnSecrets` settings in the initial request. 
* `didRegistrationMetadata` contains:
  * `walletId` is the ID of the wallet used to sign the transaction on chain.
  * `mnemonic` is the mnemonic phrase for the masterkey
* `didDocumentMetadata` shows the resolution data as it would be shown in a DID resolve request.


## Updating a PRISM DID

Updating a DID allows you to modify the DID Document on-chain – for example, to add a new key, remove a key, or change the services.
This this example we are:
- Settings a new set of services
- Adding a new verifiaction method (key) to the DID Document
- Removing a verification method (key) from the DID Document

The request body is similar to the create request, but with a few differences:
- It requires the **masterKeySecret** which is the **d** value from the master key in the DID-Creation process.
- A set of **didDocumentOperations** defining what to do with the DID Document. In this case we are using `setDidDocument`, `addToDidDocument` and `removeFromDidDocument`. For more detials on how to specifiy these operations please the the examples in code (RegistrarEndpoints.http) or the Universal Registrar API documentation.

```
POST {{OpenPrismNode_HostAddress}}/api/v1/registrar/update/did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4
Accept: application/json
Content-Type: application/json

{
  "options": {
    "storeSecretes": true,
    "returnSecrets": true,
    "walletId": "a4e8d89055c08493b297883392e730008153b6c1",
    "network": "preprod",
    "masterkeySecret": "BD7nYhBadhJqQGi20LxKoMhDgJMLLoV7FjL7ySjktdI"
  },
  "secret": {
    "verificationMethod": [
      {
        "id": "key-u1",
        "type": "JsonWebKey2020",
        "purpose": [
          "authentication"
        ],
        "curve": "secp256k1"
      },
      {
        "id": "key-u2",
        "type": "JsonWebKey2020",
        "purpose": [
          "authentication"
        ],
        "curve": "Ed25519"
      }
    ]
  },
  "didDocumentOperation": [
    "setDidDocument",
    "addToDidDocument",
    "removeFromDidDocument"
  ],
  "didDocument": [
    {
      "@context": [
        "https://www.w3.org/ns/did/v1",
        "https://w3id.org/security/suites/jws-2020/v1",
        "https://identity.foundation/.well-known/did-configuration/v1",
        "https://didcomm.org/messaging/contexts/v3"
      ],
      "service": [
        {
          "id": "service-3",
          "type": "LinkedDomains",
          "serviceEndpoint": "https://opn.blocktrust.dev/"
        }
      ]
    },
    {
      "verificationMethod": [
        {
          "id": "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4#key-u1"
        },
        {
          "id": "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4#key-u2"
        }
      ]
    },
    {
      "verificationMethod": [
        {
          "id": "did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4#key-1"
        }
      ]
    }
  ]
}
```

**Response:** *Update submitted (asynchronous)*

```json
{
  "jobId": "1c60ae3debf424c11c2ca5965b66e15de9ffe59921d4eb6ec832d811d7c3af69",
  "didState": {
    "state": "wait",
    "wait": "Please wait for the transaction to be confirmed on chain"
  }
}
```

To get the updated document you can use use the same endpoint with a get request again, similar to the create operation:

```
POST {{OpenPrismNode_HostAddress}}/api/v1/registrar/update/did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4
Accept: application/json
Content-Type: application/json

{
  "jobId": "3876002696119aeb5a7f15d538e876ce3baa8cf0726790472199bde858d9d300"
}
```


## Deactivating a PRISM DID

Deactivation renders a DID permanently inactive. After deactivation, the DID Document is no longer considered valid (and in PRISM’s case, the DID cannot be reused).
**Request:** *Deactivate a DID*

```
POST {{OpenPrismNode_HostAddress}}/api/v1/registrar/deactivate/did:prism:549eadae37ca14eb7ac73313cff2ce1ff6df8386cdf4058efd3c826cdbdcc5a4
Accept: application/json
Content-Type: application/json

{
  "options": {
    "walletId": "a4e8d89055c08493b297883392e730008153b6c1",
    "masterkeySecret": "BD7nYhBadhJqQGi20LxKoMhDgJMLLoV7FjL7ySjktdI"
    "network": "preprod"
  }
}
```


## Compatibility and Usage Notes

* **Draft Spec & Compatibility:** Since the Universal Registrar spec is still in draft, OPN’s implementation aligns with the current draft but may evolve. The core concepts (create/update/deactivate with jobIds and didState outputs) are consistent. We clearly label this as a *draft* implementation for a spec that’s not final. Clients integrating with OPN’s Registrar should be prepared for possible changes if the spec updates.

* **PRISM not officially listed yet:** The official Universal Registrar project does not yet include a did\:prism driver. did\:prism is *not* listed on the Universal Registrar’s website or default drivers as of this writing. OPN is essentially filling that gap by providing a registrar interface for PRISM. The providing of a wrapper around the existing API is not part of the current scope and is also not planed for the moment.

* **Universal Registrar adoption:** The Universal **Resolver** has seen broad adoption in the SSI community (resolving many DID methods with a single interface). The Universal **Registrar**, on the other hand, is less widely used. Many DID methods don’t have public registrar services, and many applications manage DIDs via custom SDKs or tools rather than a universal API. 

* **Internal vs Client key management:** OPN’s choice of Internal Secret Mode means ease-of-use at the cost of custody. If a user wanted full control of keys, they might want a *Client-managed Secret Mode* registrar (where the client supplies keys and signatures). Currently, OPN does not support that mode – it assumes it manages keys. This is fine for many server-side use cases or managed services. In the future, if needed, an external mode could be added, but it’s not a focus.

* **Security of returned secrets:** Because OPN by default returns the private key (`privateKeyMultibase`) in responses, clients must handle these secrets carefully. The keys are sensitive; if you’re only using OPN as a transient DID registrar, you might store these keys in your own secure vault after creation. 


## Public Deployment Considerations

Currently, OPN’s registrar is available on the Blocktrust-operated instances (for example, at `opn.mainnet.blocktrust.dev`) for demonstration and integration testing. These instances allow DID resolution without authentication, but **DID registration operations require authorization** (since they modify the ledger and use the operator’s funds). In practice, you would run your own OPN node or have access to an authorized instance to perform create/update/deactivate. The provided examples assume you have access to the API (e.g., an API key or running locally).

