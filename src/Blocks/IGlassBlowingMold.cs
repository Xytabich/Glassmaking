using Vintagestory.API.Common;

namespace GlassMaking.Blocks
{
    public interface IGlassBlowingMold
    {
        bool CanReceiveGlass(string[] layersCode, int[] layersAmount, out float fillTime);

        void TakeGlass(EntityAgent byEntity, string[] layersCode, int[] layersAmount);
    }
}