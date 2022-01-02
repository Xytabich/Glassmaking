using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace GlassMaking.Items
{
    public class ItemGlassworkPipe : Item
    {
        private Item shardsItem;
        private GlassMakingMod mod;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            mod = api.ModLoader.GetModSystem<GlassMakingMod>();
            shardsItem = api.World.GetItem(new AssetLocation("glassmaking", "glassshards"));
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            var itemstack = inSlot.Itemstack;
            var recipeAttribute = itemstack.Attributes.GetTreeAttribute("recipe");
            if(recipeAttribute != null)
            {
                var recipe = mod.GetGlassBlowingRecipe(recipeAttribute.GetString("code"));
                if(recipe != null)
                {
                    recipe.GetRecipeInfo(recipeAttribute, dsc, world, withDebugInfo);
                }
            }
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            var itemstack = inSlot.Itemstack;
            var recipeAttribute = itemstack.Attributes.GetTreeAttribute("recipe");
            if(recipeAttribute != null)
            {
                var recipe = mod.GetGlassBlowingRecipe(recipeAttribute.GetString("code"));
                if(recipe != null)
                {
                    return recipe.GetHeldInteractionHelp(recipeAttribute);
                }
            }
            return base.GetHeldInteractionHelp(inSlot);
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if(firstEvent)
            {
                var itemstack = slot.Itemstack;
                var recipeAttribute = itemstack.Attributes.GetTreeAttribute("recipe");
                if(recipeAttribute != null)
                {
                    var recipe = mod.GetGlassBlowingRecipe(recipeAttribute.GetString("code"));
                    if(recipe != null)
                    {
                        //TODO: передать renderer в котором можно будет изменить внешний вид
                        recipe.OnHeldInteractStart(slot, ref recipeAttribute, byEntity, blockSel, entitySel, firstEvent, ref handling);
                        if(recipeAttribute == null)
                        {
                            handling = EnumHandHandling.PreventDefault;
                            itemstack.Attributes.RemoveAttribute("recipe");
                            slot.MarkDirty();
                        }
                        if(handling == EnumHandHandling.PreventDefault) return;
                    }
                }
            }
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if(blockSel == null) return false;
            var itemstack = slot.Itemstack;
            var recipeAttribute = itemstack.Attributes.GetTreeAttribute("recipe");
            if(recipeAttribute != null)
            {
                var recipe = mod.GetGlassBlowingRecipe(recipeAttribute.GetString("code"));
                if(recipe != null)
                {
                    //TODO: передать renderer в котором можно будет изменить внешний вид
                    bool result = recipe.OnHeldInteractStep(secondsUsed, slot, ref recipeAttribute, byEntity, blockSel, entitySel);
                    if(recipeAttribute == null)
                    {
                        itemstack.Attributes.RemoveAttribute("recipe");
                        slot.MarkDirty();
                        return false;
                    }
                    return result;
                }
            }
            return false;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
            slot.Itemstack.TempAttributes.RemoveAttribute("lastAddGlassTime");
        }

        public override bool OnHeldAttackCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            slot.Itemstack.TempAttributes.RemoveAttribute("lastAddGlassTime");
            return base.OnHeldAttackCancel(secondsPassed, slot, byEntity, blockSelection, entitySel, cancelReason);
        }
    }
}