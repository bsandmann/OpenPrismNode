namespace OpenPrismNode.Core.Commands.CreateOperationsStatus;

using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Models;

public class CreateOperationStatusResponse()
{
    public int OperationStatusEntityId { get; set; }
    public byte[] OperationStatusId { get; set; }
}