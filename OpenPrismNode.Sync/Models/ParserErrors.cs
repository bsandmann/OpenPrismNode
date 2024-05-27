namespace OpenPrismNode.Sync.Models;

public static class ParserErrors
{
    public const string InvalidDeserializationResult = "Invalid deserialization result";
    public const string UnableToDeserializeBlockchainMetadata = "Unable to deserialize blockchain metadata";
    public const string UnsupportedPrismBlockVersion = "Unsupported Prism-Block-Version";
    public const string ParsingFailedInvalidForm = "Parsing failed. Invalid form";
    public const string UnknownOperation = "Unknown operation";
    public const string InvalidState = "Invalid state";
    public const string DataOfDidCreationCannotBeConfirmedDueToMissingKey = "Data of DID-Creation cannot be confirmed due to missing key";
    public const string UnableToVerifySignature = "Unable to verify signature";
    public const string UnableToResolveForPublicKeys = "Unable resolve DID in order to find publicKeys";
    public const string UnsupportedPrismOperationLikleyOldFormat = "Unsupported PRISM-Operation. Likley an old format";
    public const string PublicKeysNotFoundOrInvalid = "PublicKeys could not be found or are invalid (e.g. wrong length)";
    public const string InvalidVersionInProtocolUpdate = "Invalid version-data of the protocol verison update operation";
    public const string InvalidPreviousOperationHash = "Invalid previous operation hash";
    public const string OperationAlreadyOnChain = "An identical operations was already registered on chain";
    public const string KeyAlreadyAdded = "The key was already added to the did";
    public const string KeyToBeRemovedNotFound = "The key which should be removed couldn't be found";
    public const string ServiceAlreadyAdded = "The service was already added to the did";
    public const string ServiceToBeRemovedNotFound = "The service which should be removed couldn't be found";
    public const string ServiceToBeUpdatedNotFound = "The service which should be updated couldn't be found";
    public const string InvalidProtocolVersionUpdate = "Invalid protocol version update";
}