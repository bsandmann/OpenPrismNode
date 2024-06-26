namespace OpenPrismNode.Sync.Tests.TransactionEncoding;

using System.Text.Json.Nodes;
using Commands.DecodeTransaction;
using Commands.EncodeTransaction;
using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;
using TestDocuments;

public class EncodingDeactivateDid
{
    // NOTE: Roundtrip operations fail for PRISM v1, due to the removal of the optional BlockByteLength and BlockOperation Count
    // vales in the AtalaObject. These values are marked as "reserved" which prevents them from being serialized to.

    // The roundtrip also fails for PRISM v2 transactions, which happend before ~Winter 2023, since the reserved-fields have
    // only been effect around this time.

    [Fact]
    public async Task DeactivateDid_roundtrip_encoding_for_Prism_v2()
    {
        // TODO I currently cannot find a PRISM v2 deactive operation which is not a legacy operation
        // Without any I'm unable to run the rountrip test
    }
}