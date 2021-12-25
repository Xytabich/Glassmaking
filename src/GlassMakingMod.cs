using GlassMaking.Blocks;
using GlassMaking.Items;
using Vintagestory.API.Common;

namespace GlassMaking
{
    public class GlassMakingMod : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterItemClass("glassmaking:glassworkpipe", typeof(ItemGlassworkPipe));
            api.RegisterItemClass("glassmaking:glassblend", typeof(ItemGlassBlend));

            api.RegisterBlockClass("glassmaking:firebox", typeof(BlockFirebox));
            api.RegisterBlockClass("glassmaking:glasssmeltery", typeof(BlockGlassSmeltery));
            api.RegisterBlockClass("glassmaking:glassworktable", typeof(BlockGlassworktable));
            api.RegisterBlockClass("Horizontal2BMultiblockSurrogate", typeof(BlockHorizontal2BMultiblockSurrogate));
            api.RegisterBlockClass("Horizontal2BMultiblockMain", typeof(BlockHorizontal2BMultiblockMain));

            api.RegisterBlockEntityClass("glassmaking:firebox", typeof(BlockEntityFirebox));
            api.RegisterBlockEntityClass("glassmaking:glasssmeltery", typeof(BlockEntityGlassSmeltery));
            api.RegisterBlockEntityClass("glassmaking:glassmold", typeof(BlockEntityGlassBlowingMold));
            api.RegisterBlockEntityClass("glassmaking:glassworktable", typeof(BlockEntityGlassworktable));

            api.RegisterBlockBehaviorClass("Horizontal2BMultiblock", typeof(BlockBehaviorHorizontal2BMultiblock));
        }
    }
}