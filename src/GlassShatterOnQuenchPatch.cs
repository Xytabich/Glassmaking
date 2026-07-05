using GlassMaking.Common;
using HarmonyLib;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GlassMaking
{
	/// <summary>
	/// Shatters glass items into shards when thrown in water.
	/// </summary>
	[HarmonyPatch(typeof(CollectibleBehaviorQuenchable), nameof(CollectibleBehaviorQuenchable.CoolToTemperature))]
	internal static class GlassShatterOnQuenchPatch
	{
		/// <summary>Above this temperature (°C) quenching raw glass shatters it.</summary>
		private const float QuenchShatterTemperature = 100f;

		[HarmonyPrefix]
		private static bool Prefix(IWorldAccessor world, ItemSlot slot, Vec3d pos, ref bool __result)
		{
			var stack = slot?.Itemstack;
			if(stack == null || !GlassShatter.IsRawGlass(stack.Collectible)) return true;
			if(stack.Collectible.GetTemperature(world, stack) <= QuenchShatterTemperature) return true;

			if(world.Side == EnumAppSide.Server)
			{
				var shardStacks = GlassShatter.CreateShardStacks(world, stack).ToList();
				var first = shardStacks.Count > 0 ? shardStacks[0] : null;

				if(slot is EntityItemSlot eis) eis.Ei.Itemstack = first;
				else slot!.Itemstack = first;

				for(int i = 1; i < shardStacks.Count; i++)
				{
					world.SpawnItemEntity(shardStacks[i], pos);
				}
				slot!.MarkDirty();

				world.SpawnCubeParticles(pos, stack, 0.3f, 8, 0.5f);
				world.PlaySoundAt(new AssetLocation("game", "sounds/block/glass"), pos.X, pos.Y, pos.Z, null, false, 16f, 0.5f);
			}

			__result = false;
			return false;
		}
	}
}
