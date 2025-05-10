---
title: Universal Resolver API
layout: default
nav_order: 8        # lower numbers appear higher in the sidebar
---

# Resolver Overview

## What is DID Resolution?

**DID resolution** is the process of taking a Decentralized Identifier (DID) and returning the associated DID Document, along with some metadata about the operation. In simpler terms, a DID *resolver* is a component or service that takes a DID as input and produces the DID Document as output. The W3C DID Core 1.0 specification defines this process and outcome. According to the spec, resolving a DID yields a **DID resolution result** that includes:

* The **DID Document** – the JSON (or JSON-LD) document containing public keys, service endpoints, and other data describing the DID subject.
* **Resolution metadata** – information about the resolution process itself (e.g., content type, any errors or status info).
* **DID Document metadata** – additional metadata about the DID Document (e.g., timestamps, version, whether the DID is deactivated).

In fact, the DID Resolution spec formalizes the resolver’s output as three parts: `didResolutionMetadata`, `didDocument`, and `didDocumentMetadata`. The exact contents of these sections are defined by the spec (for example, an `error` code in resolution metadata if resolution fails, or a `deactivated: true` flag in document metadata if a DID is deactivated).

## Method-Specific DID Resolution

Crucially, the steps to resolve a DID depend on its **DID method**. Each DID method has its own specification that defines how a resolver should fetch or compute the DID Document. For example:

* **did\:web** – This method uses the existing web infrastructure (HTTPS and DNS) for resolution. A `did:web` DID is translated into an HTTPS URL on a domain. The DID Document is simply retrieved as a JSON file from a well-known path on that website. *In other words, resolving `did:web:example.com:users:alice` would result in an HTTPS GET request (e.g., to `https://example.com/.well-known/did.json` or a similar path) to fetch the DID Document.* No blockchain or P2P network is needed; it’s just standard web hosting of a JSON DID document.

* **did\:key** – This method is completely self-contained. All the information needed to construct the DID Document is **encoded directly in the DID itself**. A `did:key` DID string is essentially a representation of a public key (using multibase encoding). Resolving a did\:key involves no network calls at all – the resolver just decodes the DID string to reveal the DID Document (which will contain that public key as a verification method). In summary, the did\:key method “packs” everything into the identifier, trading off updatability (did\:key DIDs are typically static and cannot be updated or deactivated since they aren’t recorded on a ledger).

* *(Other DID methods will have their own approaches; for example, some use decentralized ledgers or peer-to-peer networks. The key point is that each DID method’s spec defines how resolution works for that method.)*

## did\:prism Resolution (Cardano Prism DID Method)

The **did\:prism** method has its own unique resolution process, because Prism DIDs are anchored on the Cardano blockchain. The Prism method actually supports two resolution scenarios:

* **Off-chain (long-form) DID Documents**: did\:prism allows an *optional* "short-lived" DID that includes all the data needed in the DID itself, similar in spirit to did\:key. In this case, the DID string contains an **encoded initial DID document state**. Such a DID has the form `did:prism:<initialStateHash>:<encodedInitialState>`. If a DID is in this long form, a resolver can decode the second part of the identifier to directly obtain the DID Document without contacting any external registry. This is useful in cases where an anchored DID is not required, e.g. for holders of credentials.

* **On-chain anchored DIDs**: In the common case, a Prism DID is anchored on Cardano. The DID (which in this case looks like `did:prism:<uniqueIdentifier>` with only one colon after `prism:`) corresponds to data recorded in one ore more Cardano transaction metadata. Resolving an on-chain Prism DID involves looking up a series of **DID operations** on the blockchain. The Prism DID method defines operations like **Create**, **Update**, and **Deactivate**, which are published as Cardano transactions (specifically in the transaction metadata). A Prism resolver (or alternativly called a Prism node) must scan the blockchain to find all operations relevant to the DID, then apply the method’s rules to reconstruct the latest DID Document.

In practice, the OpenPrismNode (OPN) instance will act as a **Prism DID resolver** by maintaining an index of Cardano blockchain events for did\:prism. When a DID is resolved, OPN produces the final DID Document out of the stored data. The Prism method spec outlines this clearly: a Prism node reads the blockchain operations, validates them, and builds the DID’s associated keys and services, from which a conforming DID Document is constructed. All of this is transparent to the client – from a user or application’s perspective, you simply ask the resolver for the DID Document of `did:prism:...` and the node returns the document (after doing the heavy lifting behind the scenes).

## DID Resolution Result Format (DID Document + Metadata)

No matter the method, the result of a DID resolution request is more than just the raw DID Document JSON. The response from OPN (following the W3C and DIF Universal Resolver specifications) is a JSON structure with separate sections:

* **`didDocument`** – The DID Document itself, containing the DID subject’s public keys (`verificationMethod` entries), authentication methods, service endpoints, etc., as defined by W3C DID Core. This is the primary data you’re usually after when resolving a DID. 

* **`didDocumentMetadata`** – Metadata about the DID Document. For example, for did\:prism this may include timestamps like when the DID was created and last updated on the ledger, or a flag if the DID has been deactivated. This metadata is defined by the DID spec and typically includes things that aren’t part of the DID Document’s core contents but are useful to know (such as version information or whether the document is immutable). These properties come from the DID method’s operation history. (Per the DID spec, if a DID is deactivated, the metadata **must** include `"deactivated": true`, etc.)

* **`didResolutionMetadata`** – Metadata about the resolution process itself. This often includes the `contentType` of the DID Document (e.g. `application/did+ld+json` for a JSON-LD DID document). It may also include information like whether the resolver had to redirect or any error codes if something went wrong (for example, an `error: notFound` if the DID did not exist). In a successful resolution, the OPN will typically report the media type and possibly a timestamp or driver info here. In many cases, for a normal successful resolve, this section is minimal.

The OPN adheres to the standard output format, meaning it returns **all three sections** in a single JSON response. This structure conforms to the Universal Resolver HTTP interface standard, making the OPN resolver’s output easy to integrate with other tools.

## Example: Resolving a did\:prism DID with OPN

To make this concrete, let's walk through a practical example. We will resolve the DID `did:prism:52e163e8e53466b808e53df870bccd0a066aa4d05af9b689f5c73edcbe23d625` using the OPN REST API. (This is a sample Prism DID on Cardano mainnet.)

You can use a tool like **cURL** to query the OPN resolver endpoint:

```bash
curl https://opn.mainnet.blocktrust.dev/api/v1/identifiers/did:prism:52e163e8e53466b808e53df870bccd0a066aa4d05af9b689f5c73edcbe23d625
```

The above request will return a JSON DID resolution result. It will look something like this (truncated for brevity):

```json
{
  "didDocument": {
    "@context": [
      "https://www.w3.org/ns/did/v1",
      "https://w3id.org/security/suites/ed25519-2020/v1"
    ],
    "id": "did:prism:52e163...d625",
    "verificationMethod": [
      {
        "id": "did:prism:52e163...d625#key-1",
        "type": "Ed25519VerificationKey2020",
        "controller": "did:prism:52e163...d625",
        "publicKeyMultibase": "zDhFqm...ABC123..." 
      }
    ],
    "authentication": [
      "did:prism:52e163...d625#key-1"
    ],
    "assertionMethod": [
      "did:prism:52e163...d625#key-1"
    ],
    "service": [
      {
        "id": "did:prism:52e163...d625#linked-domain",
        "type": "LinkedDomains",
        "serviceEndpoint": "https://example.com/profile"
      }
    ]
  },
  "didDocumentMetadata": {
    "created": "2023-07-21T15:23:10Z",
    "updated": "2024-05-10T11:47:00Z"
  },
  "didResolutionMetadata": {
    "contentType": "application/did+ld+json"
  }
}
```

Let’s break down the **main fields** in this response:

* **`didDocument`** – Under this, you see the actual DID Document. In the example, the DID Document has an `id` (the DID itself), and one verification method (`Ed25519VerificationKey2020`) with an embedded public key. That key is referenced in the `authentication` and `assertionMethod` arrays, meaning it can be used to authenticate as the DID subject and to sign verifiable credentials on behalf of the DID subject, respectively. There’s also a `service` endpoint in this DID Document (for illustration) – in this case a service of type `LinkedDomains` pointing to a profile URL. (Service entries are optional; they allow DIDs to advertise endpoints for interaction or additional data.)

* **`didDocumentMetadata`** – This contains metadata about the DID Document’s state. For example, `created` and `updated` timestamps tell us when the DID was initially created on-chain and when it was last updated. If the DID had been deactivated, we would also see `"deactivated": true` here (and the DID Document above would likely be mostly empty or omitted). These metadata fields are provided by the resolver based on the blockchain records. In our example, the DID was created in July 2023 and updated in May 2024, indicating there was at least one update operation after creation.

* **`didResolutionMetadata`** – This contains information about the resolution process. In the example, it shows the `contentType` as `application/did+ld+json`, confirming that the DID Document is returned in the standard JSON-LD format as per the DID Core specification. There are no error messages, since the resolution was successful. If, for instance, the DID did not exist or the method was unsupported, this section might contain an `"error"` entry (and the didDocument might be `null` in such a case). In a normal successful resolution, you’ll typically just see the content type and perhaps some resolver-specific info here.

This JSON structure conforms to the **DID resolution result** format defined in the W3C/DIF Universal Resolver spec. In other words, the output we get from OPN is exactly what a Universal Resolver-compliant client expects: a DID Document plus associated metadata sections in a JSON response.

## Universal Resolver Compatibility and Testing

OpenPrismNode’s resolver API is designed to be fully compatible with the **Universal Resolver** ecosystem. The example above demonstrates that OPN returns the standard fields (`didDocument`, `didDocumentMetadata`, `didResolutionMetadata`) as specified by the Universal Resolver interface. This means you can plug OPN into existing DID resolution tools or libraries with minimal effort. For instance, the **hosted Universal Resolver frontend** (such as the one provided by the Decentralized Identity Foundation) can use OPN’s endpoint to resolve `did:prism` DIDs — OPN acts as the driver for the `did:prism` method.

For testing and development, you don’t have to use mainnet right away. OPN is available on Cardano’s preprod as well at **[https://opn.preprod.blocktrust.dev/](https://opn.preprod.blocktrust.dev/)**. This instance is connected to Cardano’s pre-production testnet (rather than mainnet), allowing you to experiment with DID operations and resolution in a sandbox environment. You can use the same API endpoints on the preprod OPN (e.g., `/api/v1/identifiers/<did>`) to resolve test DIDs like `did:prism` DIDs created on the Cardano pre-production chain.

## Universal Resolover Development
While the Blocktrust team developed the OPN with the Universal Resolver compatible API, code to connect the OPN to the Universal Resolver is written by Fabio Pinheiro (IOG) and can be found [here](https://github.com/FabioPinheiro/uni-resolver-driver-did-prism/tree/main).
