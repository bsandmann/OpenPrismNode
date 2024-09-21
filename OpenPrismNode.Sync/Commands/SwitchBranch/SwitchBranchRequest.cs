namespace OpenPrismNode.Sync.Commands.SwitchBranch;

using FluentResults;
using MediatR;
using OpenPrismNode.Core.Models;

public class SwitchBranchRequest : IRequest<Result>
{
    public SwitchBranchRequest(LedgerType ledger, int baseBlockHeight, int baseBlockPrefix, int newTipBlockHeight, int newTipBlockPrefix)
    {
        Ledger = ledger;
        BaseBlockHeight = baseBlockHeight;
        BaseBlockPrefix = baseBlockPrefix;
        NewTipBlockHeight = newTipBlockHeight;
        NewTipBlockPrefix = newTipBlockPrefix;
    }
    
    public LedgerType Ledger { get; set; }
    public int BaseBlockHeight { get; set; } 
    public int BaseBlockPrefix { get; set; } 
    public int NewTipBlockHeight { get; set; } 
    public int NewTipBlockPrefix { get; set; } 
}