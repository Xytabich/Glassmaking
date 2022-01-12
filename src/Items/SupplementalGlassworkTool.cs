using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking.Items
{
    public class SupplementalGlassworkTool : CollectibleBehavior
    {
        private GlassMakingMod mod;
        private string toolType;

        public SupplementalGlassworkTool(CollectibleObject collObj) : base(collObj)
        {
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            mod = api.ModLoader.GetModSystem<GlassMakingMod>();
        }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            toolType = properties["tool"].AsString();
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            if(byEntity.RightHandItemSlot == slot && TryGetRecipe(byEntity, out var recipe, out var recipeAttribute, out var pipeItem))
            {
                recipe.OnHeldInteractStart(byEntity.LeftHandItemSlot, recipeAttribute, byEntity, blockSel, entitySel, firstEvent, ref handHandling, out bool isComplete);
                pipeItem.OnRecipeUpdated(byEntity.LeftHandItemSlot, isComplete);
                if(isComplete) handHandling = EnumHandHandling.PreventDefault;
                handling = EnumHandling.PreventSubsequent;
            }
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            if(byEntity.RightHandItemSlot == slot && TryGetRecipe(byEntity, out var recipe, out var recipeAttribute, out var pipeItem))
            {
                var result = recipe.OnHeldInteractStep(secondsUsed, byEntity.LeftHandItemSlot, recipeAttribute, byEntity, blockSel, entitySel, out bool isComplete);
                pipeItem.OnRecipeUpdated(byEntity.LeftHandItemSlot, isComplete);
                handling = EnumHandling.PreventSubsequent;
                return result;
            }
            return false;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            if(byEntity.RightHandItemSlot == slot && TryGetRecipe(byEntity, out var recipe, out var recipeAttribute, out _))
            {
                recipe.OnHeldInteractStop(secondsUsed, byEntity.LeftHandItemSlot, recipeAttribute, byEntity, blockSel, entitySel);
                handling = EnumHandling.PreventSubsequent;
            }
        }

        private bool TryGetRecipe(EntityAgent byEntity, out GlassBlowingRecipe recipe, out ITreeAttribute recipeAttribute, out ItemGlassworkPipe pipeItem)
        {
            pipeItem = byEntity.LeftHandItemSlot?.Itemstack?.Item as ItemGlassworkPipe;
            if(pipeItem != null)
            {
                recipeAttribute = byEntity.LeftHandItemSlot.Itemstack.Attributes.GetTreeAttribute("recipe");
                if(recipeAttribute != null)
                {
                    recipe = mod.GetGlassBlowingRecipe(recipeAttribute.GetString("code"));
                    if(recipe != null)
                    {
                        var step = recipe.GetStep(recipeAttribute);
                        if(step != null && step.tool == toolType)//TODO: check damage
                        {
                            return true;
                        }
                    }
                }
            }

            recipe = null;
            recipeAttribute = null;
            return false;
        }
    }
}