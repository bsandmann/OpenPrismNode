namespace OpenPrismNode.Core.Models;

public enum PrismKeyUsage
{
    /// <summary>
    /// UNKNOWN_KEY is an invalid value - Protobuf uses 0 if no value is provided and we want the user to explicitly choose the usage.
    /// </summary>
    UnknownKey = 0,

    /// <summary>
    /// This is the most privileged key-type, when any other key is lost, you could use this to recover the others.
    /// </summary>
    MasterKey = 1,

    /// <summary>
    /// This key-type is used for issuing credentials only, it should be kept in a safe place
    /// to avoid malicious credentials being issued.
    /// </summary>
    IssuingKey = 2,

    /// <summary>
    /// This key-type is used for end-to-end encrypted communication, whoever wants to send a message should
    /// use this key-type to encrypt the jsonContent.
    /// </summary>
    KeyAgreementKey = 3,

    /// <summary>
    /// This key-type is used to authenticate requests or logging into services.
    /// </summary>
    AuthenticationKey = 4,

    /// <summary>
    /// This key-type is used for revoking credentials only, it should be kept in a safe place
    /// to avoid malicious credentials being issued.
    /// </summary>
    RevocationKey = 5,
    CapabilityInvocationKey = 6,
    CapabilityDelegationKey = 7,
}