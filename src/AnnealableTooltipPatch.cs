using System.Text;
using GlassMaking.Common;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace GlassMaking
{
	/// <summary>
	/// Appends a warning about annealing requirements to applicable items.
	/// </summary>
	internal static class AnnealTooltip
	{
		public static void Append(ItemSlot inSlot, StringBuilder dsc)
		{
			if(GlassShatter.IsRawGlass(inSlot?.Itemstack?.Collectible))
			{
				dsc.AppendLine(Lang.Get("glassmaking:Needs annealing or will shatter"));
			}
		}
	}

	[HarmonyPatch(typeof(Block), nameof(Block.GetHeldItemInfo))]
	internal static class BlockAnnealTooltipPatch
	{
		[HarmonyPostfix]
		private static void Postfix(ItemSlot inSlot, StringBuilder dsc) => AnnealTooltip.Append(inSlot, dsc);
	}

	[HarmonyPatch(typeof(Item), nameof(Item.GetHeldItemInfo))]
	internal static class ItemAnnealTooltipPatch
	{
		[HarmonyPostfix]
		private static void Postfix(ItemSlot inSlot, StringBuilder dsc) => AnnealTooltip.Append(inSlot, dsc);
	}
}
