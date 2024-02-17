using GlassMaking.Items.Behavior;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace GlassMaking.Items
{
	public class ItemGlassworkPipe : ItemGlassContainer
	{
		public GlasspipeCraftBehavior[] GlasspipeBehaviors => glasspipeBehaviors;

		internal ModelTransform glassTransform;

		private GlassMakingMod mod;
		private CollectibleBehavior[] prioritizedBehaviors;
		private GlasspipeCraftBehavior[] glasspipeBehaviors;

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
			prioritizedBehaviors = (CollectibleBehavior[])CollectibleBehaviors.Clone();
			SortBehaviors(prioritizedBehaviors);
			glasspipeBehaviors = prioritizedBehaviors.Select(b => b as GlasspipeCraftBehavior).Where(b => b != null).ToArray();

			mod = api.ModLoader.GetModSystem<GlassMakingMod>();
			glassTransform = Attributes["glassTransform"].AsObject<ModelTransform>();
			glassTransform.EnsureDefaultValues();
		}

#nullable enable
		public GlasspipeCraftBehavior? GetActiveCraft(ItemStack itemStack)
		{
			foreach(var beh in glasspipeBehaviors)
			{
				if(beh.IsActive(itemStack)) return beh;
			}
			return null;
		}
#nullable disable

		public override bool MatchesForCrafting(ItemStack inputStack, GridRecipe gridRecipe, CraftingRecipeIngredient ingredient)
		{
			EnumHandling handling;
			bool preventDefault = false;
			foreach(var beh in prioritizedBehaviors)
			{
				if(beh is ICraftingGridBehavior behavior)
				{
					handling = EnumHandling.PassThrough;
					bool result = behavior.MatchesForCrafting(inputStack, gridRecipe, ingredient, ref handling);
					if(handling != EnumHandling.PassThrough)
					{
						if(result) return true;
						if(handling == EnumHandling.PreventDefault)
						{
							preventDefault = true;
						}
						else if(handling == EnumHandling.PreventSubsequent)
						{
							return false;
						}
					}
				}
			}
			if(preventDefault) return false;
			return base.MatchesForCrafting(inputStack, gridRecipe, ingredient);
		}

		public override void OnConsumedByCrafting(ItemSlot[] allInputSlots, ItemSlot stackInSlot, GridRecipe gridRecipe, CraftingRecipeIngredient fromIngredient, IPlayer byPlayer, int quantity)
		{
			EnumHandling handling;
			bool preventDefault = false;
			foreach(var beh in prioritizedBehaviors)
			{
				if(beh is ICraftingGridBehavior behavior)
				{
					handling = EnumHandling.PassThrough;
					behavior.OnConsumedByCrafting(allInputSlots, stackInSlot, gridRecipe, fromIngredient, byPlayer, quantity, ref handling);
					if(handling == EnumHandling.PreventDefault)
					{
						preventDefault = true;
					}
					else if(handling == EnumHandling.PreventSubsequent)
					{
						return;
					}
				}
			}
			if(preventDefault) return;
			base.OnConsumedByCrafting(allInputSlots, stackInSlot, gridRecipe, fromIngredient, byPlayer, quantity);
		}

		public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
		{
			foreach(var behavior in prioritizedBehaviors)
			{
				var handled = EnumHandling.PassThrough;
				behavior.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling, ref handled);
				if(handled == EnumHandling.PreventSubsequent) return;
			}
		}

		public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
		{
			bool result = true;
			bool preventDefault = false;

			foreach(CollectibleBehavior behavior in prioritizedBehaviors)
			{
				EnumHandling handled = EnumHandling.PassThrough;

				bool behaviorResult = behavior.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handled);

				if(handled == EnumHandling.PreventSubsequent) return behaviorResult;
				if(handled != EnumHandling.PassThrough)
				{
					result &= behaviorResult;
					preventDefault = true;
				}
			}

			if(preventDefault) return result;
			return false;
		}

		public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
		{
			bool result = true;
			bool preventDefault = false;

			foreach(CollectibleBehavior behavior in prioritizedBehaviors)
			{
				EnumHandling handled = EnumHandling.PassThrough;

				bool behaviorResult = behavior.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handled);

				if(handled == EnumHandling.PreventSubsequent) return behaviorResult;
				if(handled != EnumHandling.PassThrough)
				{
					result &= behaviorResult;
					preventDefault = true;
				}
			}

			if(preventDefault) return result;
			return true;
		}

		public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
		{
			foreach(var behavior in prioritizedBehaviors)
			{
				var handled = EnumHandling.PassThrough;
				behavior.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handled);
				if(handled == EnumHandling.PreventSubsequent) return;
			}
		}

		public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
		{
			return null;
		}

		public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
		{
			EnumHandling handling;
			bool preventDefault = false;
			foreach(var beh in prioritizedBehaviors)
			{
				if(beh is IRenderBehavior behavior)
				{
					handling = EnumHandling.PassThrough;
					behavior.OnBeforeRender(capi, itemstack, target, ref renderinfo, ref handling);
					if(handling == EnumHandling.PreventSubsequent) return;
					if(handling == EnumHandling.PreventDefault) preventDefault = true;
				}
			}
			if(preventDefault) return;

			mod.itemsRenderer.RemoveRenderer(itemstack);
			base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
		}

		public void ChangeGlassTemperature(IWorldAccessor world, ItemStack item, int totalGlass, int addedGlass, float addedGlassTemperature)
		{
			if(totalGlass == addedGlass)
			{
				SetGlassTemperature(world, item, addedGlassTemperature);
			}
			else
			{
				SetGlassTemperature(world, item, GameMath.Lerp(GetGlassTemperature(world, item), addedGlassTemperature, (float)addedGlass / totalGlass));
			}
		}

		private static void SortBehaviors(CollectibleBehavior[] behaviors)
		{
			var compareInfo = new (int index, double priority)[behaviors.Length];
			for(int i = behaviors.Length - 1; i >= 0; i--)
			{
				compareInfo[i] = (i, (behaviors[i] as IPrioritizedBehavior)?.Priority ?? 0);
			}
			Array.Sort(compareInfo, behaviors, new PriorityComparer());
		}

		private class PriorityComparer : IComparer<(int index, double priority)>
		{
			int IComparer<(int index, double priority)>.Compare((int index, double priority) x, (int index, double priority) y)
			{
				int c = y.priority.CompareTo(x.priority);// higher value - lower index
				if(c != 0) return c;
				return x.index.CompareTo(y.index);
			}
		}

		public interface IMeshContainer
		{
			MeshData Mesh { get; }

			void BeginMeshChange();

			void EndMeshChange();
		}
	}
}