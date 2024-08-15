namespace OpenPrismNode.Sync.Commands.ProcessBlock;

public class ProcessBlockResponse
{
    public ProcessBlockResponse(byte[] previousBlockHash, int previousBlockHeight)
    {
        PreviousBlockHash = previousBlockHash;
        PreviousBlockHeight = previousBlockHeight;
    }

    public byte[] PreviousBlockHash { get; }
    public int PreviousBlockHeight { get; }
}