namespace OpenPrismNode.Sync.Models;

using FluentResults;

public static class ParserErrors
{
    public const string InvalidDeserializationResult = "Invalid deserialization result";
    public const string UnableToDeserializeBlockchainMetadata = "Unable to deserialize blockchain metadata";
    public const string UnsupportedPrismBlockVersion = "Unsupported Prism-Block-Version";
    public const string ParsingFailedInvalidForm = "Parsing failed. Invalid form";
    public const string UnknownOperation = "Unknown operation";
    public const string UnsupportedOperation = "Unsupported operation";
    public const string InvalidState = "Invalid state";
    public const string DataOfDidCreationCannotBeConfirmedDueToMissingKey = "Data of DID-Creation cannot be confirmed due to missing key";
    public const string UnableToVerifySignature = "Unable to verify signature";
    public const string UnableToResolveForPublicKeys = "Unable resolve DID in order to find publicKeys";
    public const string UnsupportedPrismOperationLikleyOldFormat = "Unsupported PRISM-Operation. Likley an old format";
    public const string PublicKeysNotFoundOrInvalid = "PublicKeys could not be found or are invalid (e.g. wrong length)";
    public const string InvalidVersionInProtocolUpdate = "Invalid version-data of the protocol verison update operation";
    public const string InvalidPreviousOperationHash = "Invalid previous operation hash";
    public const string OperationAlreadyOnChain = "An identical operations was already registered on chain";
    public const string KeyAlreadyAdded = "The key was already added to the DID";
    public const string KeyToBeRemovedNotFound = "The key which should be removed couldn't be found";
    public const string ServiceAlreadyAdded = "The service was already added to the DID";
    public const string ServiceAlreadyRemoved = "The service was already removed from the DID in a previous action";
    public const string ServiceToBeRemovedNotFound = "The service which should be removed couldn't be found or has already been removed";
    public const string ServiceToBeUpdatedNotFound = "The service which should be updated couldn't be found or has already been removed";
    public const string InvalidProtocolVersionUpdate = "Invalid protocol version update";
    public const string NoPublicKeyFound = "No public keys found in the operation. At least one public key is required";
    public const string MaxVerifiactionMethodNumber = "Public key number exceeds the maximum allowed number of verification methods according to the global PrismParameters";
    public const string NoMasterKey = "No master key found in the createDid operation. At least one master key is required";
    public const string DuplicateKeyIds = "Duplicate key IDs detected. Each key ID must be unique";
    public const string DuplicateServiceIds = "Duplicate service Ids detected. Each key Id must be unique";
    public const string MaximumKeyIdSize = "KeyId exceeds the maximum allowed size according to the global PrismParameters";
    public const string MaxServiceNumber = "Service number exceeds the maximum allowed number of services according to the global PrismParameters";
    public const string ServiceTypeInvalid = "Service type is not valid. It must not be empty, must not contain leading or trailing whitespaces and must not exceed the maximum allowed size according to the global PrismParameters";
    public const string UnexpectedServiceType = "Service type is not in the list of expected service types";
    public const string InvalidServiceId = "Service id is not valid. It must not be empty and must not exceed the maximum allowed size according to the global PrismParameters";
    public const string ServiceEndpointInvalid = "Service endpoint is not valid. It must not exceed the maximum allowed size";
    public const string KeyAlreadyRemoved = "The key was already removed from the DID";
    public const string KeyAlreadyRemovedInPreviousAction = "The key was already removed from in a previous action";
    public const string UpdateOperationMasterKey = "After the update operation at least one valid master key must exist. The last master key cannot be removed";
    public const string MasterKeyMustBeSecp256k1 = "The master key must be a secp256k1" ;
}