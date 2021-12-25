using Vintagestory.API.Common;

namespace GlassMaking.Blocks
{
    public interface IGlassBlowingMold
    {
        bool CanReceiveGlass(int count);

        int GetRequiredAmount();

        ItemStack GetOutputItem();
    }
}