using Vintagestory.API.Common;

namespace GlassMaking.Blocks
{
    public class BlockEntityGlassBlowingMold : BlockEntity, IGlassBlowingMold
    {
        private int requiredUnits;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if(Block != null && Block.Code != null && Block.Attributes != null)
            {
                requiredUnits = Block.Attributes["requiredUnits"].AsInt();
            }
        }

        public bool CanReceiveGlass(int count)
        {
            return count >= requiredUnits;
        }

        public int GetRequiredAmount()
        {
            return requiredUnits;
        }

        public ItemStack GetOutputItem()
        {
            var jstack = Block.Attributes["output"].AsObject<JsonItemStack>();
            jstack.Resolve(Api.World, "glass mold output for " + Block.Code);
            return jstack.ResolvedItemstack;
        }
    }
}