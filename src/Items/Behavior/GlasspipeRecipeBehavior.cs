using GlassMaking.GenericItemAction;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace GlassMaking.Items.Behavior
{
	public class GlasspipeRecipeBehavior : GlasspipeCraftBehavior, IItemCrafter, ICraftingGridBehavior, IGenericHeldItemAction, IRenderBehavior
	{
		private const string ATTRIB_KEY = "glassmaking:recipe";

		public override double Priority => 1;

		public GlasspipeRecipeBehavior(CollectibleObject collObj) : base(collObj)
		{
		}

		public override bool IsActive(ItemStack itemStack)
		{
			return itemStack.Attributes.HasAttribute(ATTRIB_KEY);
		}

		public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
		{
			if(glassMaking.GetGlassBlowingRecipes().Count > 0)
			{
				var beh = glassworkPipe.GetActiveCraft(inSlot.Itemstack);
				if(beh == null)
				{
					return new WorldInteraction[] {
						new WorldInteraction() {
							ActionLangCode = "glassmaking:heldhelp-glasspipe-recipe",
							HotKeyCode = GlassMakingMod.RECIPE_SELECT_HOTKEY,
							MouseButton = EnumMouseButton.None
						}
					};
				}
				else if(beh == this && IsHeated(api.World, inSlot.Itemstack))
				{
					var interactions = new List<WorldInteraction>();
					var itemStack = inSlot.Itemstack;
					var recipe = GetRecipe(itemStack, out var recipeAttribute);
					recipe?.GetInteractionHelp(itemStack, recipeAttribute, interactions, api.World, glassMaking);
					return interactions.ToArray();
				}
			}
			return Array.Empty<WorldInteraction>();
		}

		public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
			base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
			var itemStack = inSlot.Itemstack;
			var recipe = GetRecipe(itemStack, out var recipeAttribute);
			if(recipe != null)
			{
				recipe.GetRecipeInfo(itemStack, recipeAttribute, dsc, world, withDebugInfo);

				dsc.AppendLine(Lang.Get("Temperature: {0}°C", glassworkPipe.GetGlassTemperature(world, itemStack).ToString("0")));

				var items = new List<ItemStack>();
				recipe.GetBreakDrops(itemStack, recipeAttribute, world, items);
				if(items.Count > 0)
				{
					dsc.AppendLine();
					dsc.AppendLine(Lang.Get("glassmaking:Break down to receive:"));
					foreach(var item in items)
					{
						dsc.AppendFormat("• {0}x {1}", item.StackSize, item.GetName()).AppendLine();
					}
				}
			}
		}

		public void OnBeforeRender(ICoreClientAPI capi, ItemStack itemStack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo, ref EnumHandling handling)
		{
			var recipe = GetRecipe(itemStack, out var recipeAttribute);
			if(recipe != null)
			{
				var temperature = GlassRenderUtil.TemperatureToState(glassworkPipe.GetGlassTemperature(capi.World, itemStack), GetWorkingTemperature(capi.World, itemStack));
				glassMaking.itemsRenderer.RenderItem<PipeRecipeRenderer, PipeRecipeRenderer.Data>(
					capi,
					itemStack,
					new PipeRecipeRenderer.Data(temperature, recipe, recipeAttribute),
					ref renderinfo
				);
				handling = EnumHandling.PreventSubsequent;
			}
		}

		public bool MatchesForCrafting(ItemStack inputStack, GridRecipe gridRecipe, CraftingRecipeIngredient ingredient, ref EnumHandling handling)
		{
			if(gridRecipe.Output.ResolvedItemstack?.Item is ItemGlassworkPipe &&
				ingredient.ResolvedItemstack?.Item is ItemGlassworkPipe &&
				gridRecipe.Attributes?.IsTrue("breakglass") == true)
			{
				handling = EnumHandling.Handled;
				return inputStack.Attributes.HasAttribute(ATTRIB_KEY);
			}
			return false;
		}

		public void OnConsumedByCrafting(ItemSlot[] allInputSlots, ItemSlot stackInSlot, GridRecipe gridRecipe, CraftingRecipeIngredient fromIngredient, IPlayer byPlayer, int quantity, ref EnumHandling handling)
		{
			if(gridRecipe.Output.ResolvedItemstack?.Item is ItemGlassworkPipe && gridRecipe.Attributes?.IsTrue("breakglass") == true)
			{
				if(api.Side == EnumAppSide.Server)
				{
					var itemStack = stackInSlot.Itemstack;
					var recipe = GetRecipe(itemStack, out var recipeAttribute);
					if(recipe != null)
					{
						handling = EnumHandling.Handled;

						var entity = byPlayer?.Entity;
						if(entity != null)
						{
							var items = new List<ItemStack>();
							recipe.GetBreakDrops(itemStack, recipeAttribute, entity.World, items);
							foreach(var item in items)
							{
								item.StackSize *= quantity;
								if(!entity.TryGiveItemStack(item))
								{
									entity.World.SpawnItemEntity(item, byPlayer!.Entity.Pos.XYZ.Add(0.0, 0.5, 0.0));
								}
							}
						}
					}
				}
			}
		}

		public bool TryGetRecipeAttribute(ItemStack itemstack, out ITreeAttribute recipeAttribute)
		{
			recipeAttribute = itemstack.Attributes.GetTreeAttribute(ATTRIB_KEY);
			return recipeAttribute != null;
		}

		public void OnRecipeUpdated(ItemSlot slot, bool isComplete)
		{
			if(isComplete)
			{
				slot.Itemstack.Attributes.RemoveAttribute(ATTRIB_KEY);
				glassworkPipe.ResetGlassTemperature(api.World, slot.Itemstack);
				slot.MarkDirty();
			}
		}

		bool IItemCrafter.PreventRecipeAssignment(IClientPlayer player, ItemStack item)
		{
			return glassworkPipe.GetActiveCraft(item) != null;
		}

		bool IItemCrafter.TryGetRecipeOutputs(IClientPlayer player, ItemStack item, [NotNullWhen(true)] out KeyValuePair<IAttribute, ItemStack>[]? recipeOutputs)
		{
			var recipes = glassMaking.GetGlassBlowingRecipes();
			recipeOutputs = default;
			if(recipes.Count == 0) return false;

			recipeOutputs = new KeyValuePair<IAttribute, ItemStack>[recipes.Count];
			int index = 0;
			foreach(var pair in recipes)
			{
				recipeOutputs[index++] = new KeyValuePair<IAttribute, ItemStack>(new StringAttribute(pair.Key), pair.Value.Output.ResolvedItemstack);
			}
			return index > 0;
		}

		bool IGenericHeldItemAction.GenericHeldItemAction(IPlayer player, string action, ITreeAttribute? attributes)
		{
			if(action == "recipe")
			{
				var code = attributes?.GetString("key");
				if(!string.IsNullOrEmpty(code))
				{
					var recipe = glassMaking.GetGlassBlowingRecipe(code);
					if(recipe != null)
					{
						var slot = player.InventoryManager.ActiveHotbarSlot;
						if(slot?.Itemstack != null)
						{
							if(glassworkPipe.GetActiveCraft(slot.Itemstack) == null)
							{
								slot.Itemstack.Attributes.GetOrAddTreeAttribute(ATTRIB_KEY).SetString("code", code);
								slot.MarkDirty();
								return true;
							}
						}
					}
				}
			}
			return false;
		}

		public override bool IsHeated(IWorldAccessor world, ItemStack itemStack)
		{
			return glassworkPipe.GetGlassTemperature(world, itemStack) >= GetWorkingTemperature(world, itemStack) * 0.45f;
		}

		public override bool IsWorkingTemperature(IWorldAccessor world, ItemStack item)
		{
			return glassworkPipe.GetGlassTemperature(world, item) >= GetWorkingTemperature(world, item);
		}

		private float GetWorkingTemperature(IWorldAccessor world, ItemStack itemStack)
		{
			return GetRecipe(itemStack, out var recipeAttribute)?.GetWorkingTemperature(itemStack, recipeAttribute, world) ?? 0;
		}

		private GlassBlowingRecipe? GetRecipe(ItemStack itemStack, out ITreeAttribute recipeAttribute)
		{
			recipeAttribute = itemStack.Attributes.GetTreeAttribute(ATTRIB_KEY);
			if(recipeAttribute != null)
			{
				return glassMaking.GetGlassBlowingRecipe(recipeAttribute.GetString("code"));
			}
			return null;
		}
	}
}