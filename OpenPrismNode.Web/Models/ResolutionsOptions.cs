using System;

namespace OpenPrismNode.Core.Models
{
    public class ResolutionOptions
    {
        /// <summary>
        /// Request that caching is disabled and a fresh DID document is retrieved.
        /// </summary>
        // public bool NoCache { get; set; }

        /// <summary>
        /// The version ID of the DID document to resolve.
        /// </summary>
        public string? VersionId { get; set; }

        /// <summary>
        /// The timestamp of the specific version of the DID document to resolve.
        /// </summary>
        public DateTime? VersionTime { get; set; }
    }
}