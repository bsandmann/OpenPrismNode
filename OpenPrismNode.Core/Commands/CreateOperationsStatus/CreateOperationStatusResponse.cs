namespace OpenPrismNode.Core.Commands.CreateOperationsStatus;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Models;

public class CreateOperationStatusResponse()
{
    public int OperationStatusEntityId { get; init; }
    public byte[] OperationStatusId { get; init; }
}