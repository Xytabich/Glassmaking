using GlassMaking.Common;
using Vintagestory.API.Common;

namespace GlassMaking.Blocks
{
    public class BlockEntityGlassBlowingMold : BlockEntity, IGlassBlowingMold
    {
        private GlassMoldRecipe recipe = null;

        public bool CanReceiveGlass(string[] layersCode, int[] layersAmount, out float fillTime)
        {
            fillTime = GetRecipe().fillTime;
            var recipe = GetRecipe().recipe;
            int layerIndex = layersCode.Length - 1;
            for(int i = recipe.Length - 1; i >= 0; i--)
            {
                if(recipe[i].code.ToShortString() != layersCode[layerIndex]) return false;
                if(!recipe[i].IsSuitable(layersAmount[layerIndex])) return false;
                layerIndex--;
            }
            return true;
        }

        public void TakeGlass(EntityAgent byEntity, string[] layersCode, int[] layersAmount)
        {
            var recipe = GetRecipe().recipe;
            int layerIndex = layersCode.Length - 1;
            for(int i = recipe.Length - 1; i >= 0; i--)
            {
                layersAmount[layerIndex] -= recipe[i].amount;
            }
            var jstack = GetRecipe().output;
            if(jstack.ResolvedItemstack == null)
            {
                jstack.Resolve(Api.World, "glass mold output for " + Block.Code);
            }
            var item = jstack.ResolvedItemstack;
            if(!byEntity.TryGiveItemStack(item))
            {
                byEntity.World.SpawnItemEntity(item, byEntity.Pos.XYZ.Add(0.0, 0.5, 0.0));
            }
        }

        private GlassMoldRecipe GetRecipe()
        {
            if(recipe == null) recipe = Block.Attributes?["glassmold"]?.AsObject<GlassMoldRecipe>();
            return recipe;
        }
    }
}