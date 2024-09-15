namespace OpenPrismNode.Core.Models;

public enum OperationStatusEnum
{
    UnknownOperation = 0,
    PendingSubmission = 1,
    AwaitConfirmation = 2,
    ConfirmedAndApplied = 3,
    ConfirmedAndRejected = 4
}