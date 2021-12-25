using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking
{
    internal static class Utils
    {
        public static bool IsBlockLoaded(this IBlockAccessor accessor, BlockPos pos)
        {
            return accessor.GetChunkAtBlockPos(pos) != null;
        }
    }
}