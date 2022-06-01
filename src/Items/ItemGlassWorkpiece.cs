using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace GlassMaking.Items
{
	public class ItemGlassWorkpiece : Item
	{
		private GlassMakingMod mod;

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
			mod = api.ModLoader.GetModSystem<GlassMakingMod>();
		}

		public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
			base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
			var recipeInfo = inSlot.Itemstack.Attributes.GetTreeAttribute("glassmaking:recipe");
			if(recipeInfo != null)
			{
				var recipe = mod.GetWorkbenchRecipe(recipeInfo.GetString("code"));
				if(recipe != null)
				{
					dsc.AppendLine(Lang.Get("glassmaking:Step {0}/{1}", recipeInfo.GetInt("step", 0) + 1, recipe.Steps.Length));
				}
			}
		}

		public override string GetHeldItemName(ItemStack itemStack)
		{
			var recipeInfo = itemStack.Attributes.GetTreeAttribute("glassmaking:recipe");
			if(recipeInfo != null)
			{
				var recipe = mod.GetWorkbenchRecipe(recipeInfo.GetString("code"));
				if(recipe != null)
				{
					return Lang.Get("glassmaking:{0} (Workpiece)", recipe.Output.ResolvedItemstack.Collectible.GetHeldItemName(recipe.Output.ResolvedItemstack));
				}
			}
			return base.GetHeldItemName(itemStack);
		}

		public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
		{
			var recipeInfo = itemstack.Attributes.GetTreeAttribute("glassmaking:recipe");
			if(recipeInfo != null)
			{
				var recipe = mod.GetWorkbenchRecipe(recipeInfo.GetString("code"));
				if(recipe != null)
				{
					mod.itemsRenderer.RenderItem<GlassWorkpieceRenderer, GlassWorkpieceRenderer.Data>(capi, itemstack,
						new GlassWorkpieceRenderer.Data(recipeInfo.GetString("code"), recipeInfo.GetInt("step", 0), recipe), ref renderinfo);
				}
			}
		}
	}
}