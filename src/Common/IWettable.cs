using Vintagestory.API.Common;

namespace GlassMaking.Common
{
    public interface IWettable
    {
        float GetHumidity(ItemStack itemStack, IWorldAccessor world);

        void SetHumidity(ItemStack itemStack, float value);

        void ConsumeHumidity(ItemStack itemStack, float value, IWorldAccessor world);
    }
}