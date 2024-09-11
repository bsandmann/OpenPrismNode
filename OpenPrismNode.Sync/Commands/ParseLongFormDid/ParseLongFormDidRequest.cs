namespace OpenPrismNode.Sync.Commands.ParseLongFormDid;

using Core.Models;
using FluentResults;
using MediatR;
using OpenPrismNode.Core.Parser;

public class ParseLongFormDidRequest : IRequest<Result<InternalDidDocument>>
{
    public ParseLongFormDidRequest(ParsedDidUrl parsedDidUrl)
    {
        ParsedDidUrl = parsedDidUrl;
    }
    public ParsedDidUrl ParsedDidUrl { get; set; } 
}