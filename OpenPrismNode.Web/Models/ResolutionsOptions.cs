namespace OpenPrismNode.Web.Models
{
    using System;

    public class ResolutionOptions
    {
        /// <summary>
        /// Request that caching is disabled and a fresh DID document is retrieved.
        /// </summary>
        // public bool NoCache { get; set; }

        /// <summary>
        /// The version ID of the DID document to resolve in HEX format.
        /// Note: for the initial createDid-operation the versionId is identical to the did-identifier
        /// </summary>
        public string? VersionId { get; set; }

        /// <summary>
        /// The timestamp of the specific version of the DID document to resolve.
        /// The timestamp acts as an upper bound for the version of the DID document to resolve.
        /// e.g. 2024-01-01T00:00:00Z
        /// </summary>
        public DateTime? VersionTime { get; set; }

        /// <summary>
        /// Shows the network-identifier in the DID-Document e.g. did:prism:mainnet:123
        /// Overwrites the default setting of ledger 
        /// </summary>
        public bool? IncludeNetworkIdentifier { get; set; }
    }
}