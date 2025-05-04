using OpenPrismNode.Core.Models;
using System;

namespace OpenPrismNode.Core.Commands.GetDidList
{
    /// <summary>
    /// Response item for a single DID
    /// </summary>
    public class DidListResponseItem
    {
        /// <summary>
        /// The operation hash of the DID creation operation
        /// </summary>
        public string Did { get; set; }
        
        /// <summary>
        /// Timestamp when the DID was created
        /// </summary>
        public DateTime Time { get; set; }
        
        /// <summary>
        /// Block height of the transaction containing the DID
        /// </summary>
        public long BlockHeight { get; set; }
    }
}