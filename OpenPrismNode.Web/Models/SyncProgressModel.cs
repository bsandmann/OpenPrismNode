namespace OpenPrismNode.Web.Models;

public record SyncProgressModel
{
    public SyncProgressModel(int blockHeightDbSync, int blockHeightOpn)
    {
        BlockHeightDbSync = blockHeightDbSync;
        BlockHeightOpn = blockHeightOpn;
        if (BlockHeightOpn == BlockHeightDbSync)
        {
            IsInSync = true;
        }
    }

    public bool IsInSync { get; }
    public int BlockHeightDbSync { get; }
    public int BlockHeightOpn { get; }
}