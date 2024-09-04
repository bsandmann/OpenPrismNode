namespace OpenPrismNode.Core.Models;

using System.Diagnostics;
using Parser;

/// <summary>
/// This is used when the DID identifier is either not recognized or it is treated as a generic DID identifier.
/// The identifier URN still need to conform with what is expected from a DID identifier and a URN.
/// </summary>
/// <remarks>The DID method specific identifiers should inherit from this. They may provide more granular constructors
/// with DID method specific parameters and functionality.</remarks>
[DebuggerDisplay("{Id}")]
public record GenericDidId
{
    /// <summary>
    /// The full DID identifier string.
    /// </summary>
    public string Id { get; private init; }

    public string? MethodName { get; }
   
    public string? Network { get; }
    public string? SubNetwork { get; }
    public string? MethodSpecificId { get; }

    public bool IsInvalidDid { get; }

    /// <summary>
    /// Creates a new instance of <see cref="GenericDidId"/>. 
    /// </summary>
    /// <param name="id">The full DID identifier string.</param>
    public GenericDidId(string id)
    {
        Id = id;
        if (DidUrlParser.TryParse(id, out ParsedDidUrl parsedDidUrl))
        {
            MethodName = parsedDidUrl.MethodName;
            Network = parsedDidUrl.Network;
            SubNetwork = parsedDidUrl.SubNetwork;
            MethodSpecificId = parsedDidUrl.MethodSpecificId;
        }
        else
        {
            IsInvalidDid = true;
        }
    }

    /// <summary>
    /// Implicit conversion from <see cref="GenericDidId"/> or derived DID methods to <see langword="string"/>.
    /// </summary>
    /// <param name="didId"></param>
    public static implicit operator string(GenericDidId didId) => didId.Id;


    /// <summary>
    /// Explicit conversion from <see langword="string"/> to <see cref="GenericDidId"/> or derived DID methods.
    /// </summary>
    /// <param name="didId"></param>
    public static explicit operator GenericDidId(string didId) => new(didId);


    /// <inheritdoc/>
    public override string ToString() => Id;
}