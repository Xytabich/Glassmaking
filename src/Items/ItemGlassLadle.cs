using GlassMaking.Blocks;
using GlassMaking.Common;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace GlassMaking.Items
{
	public class ItemGlassLadle : ItemGlassContainer
	{
		public ModelTransform glassTransform;
		public int maxGlassAmount;

		private GlassMakingMod mod;
		private int amountThreshold;
		private WorldInteraction[] interactions;

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
			mod = api.ModLoader.GetModSystem<GlassMakingMod>();
			maxGlassAmount = Attributes["maxGlass"].AsInt();
			amountThreshold = Attributes["glassThreshold"].AsInt();
			glassTransform = Attributes["glassTransform"].AsObject<ModelTransform>();
			glassTransform.EnsureDefaultValues();
			if(api.Side == EnumAppSide.Client)
			{
				interactions = ObjectCacheUtil.GetOrCreate(api, "glassmaking:heldhelp-ladle", () => {
					List<ItemStack> list = new List<ItemStack>();
					var capi = api as ICoreClientAPI;
					foreach(Block block in api.World.Blocks)
					{
						if(block is IGlassmeltSourceBlock)
						{
							List<ItemStack> stacks = block.GetHandBookStacks(capi);
							if(stacks != null) list.AddRange(stacks);
						}
					}
					return new WorldInteraction[] {
						new WorldInteraction() {
							ActionLangCode = "glassmaking:heldhelp-ladle-intake",
							MouseButton = EnumMouseButton.Right,
							Itemstacks = list.ToArray()
						}
					};
				});
			}
		}

		public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
		{
			return interactions.Append(base.GetHeldInteractionHelp(inSlot));
		}

		public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
			base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
			var itemstack = inSlot.Itemstack;
			var glassmelt = itemstack.Attributes.GetTreeAttribute("glassmelt");
			if(glassmelt != null)
			{
				var code = new AssetLocation(glassmelt.GetString("code"));
				var amount = glassmelt.GetInt("amount");

				dsc.AppendFormat(IsWorkingTemperature(world, itemstack) ? "Contains {0} units of molten {1} glass" : "Contains {0} units of {1} glass", amount, Lang.Get(GlassBlend.GetBlendNameCode(code))).AppendLine();
				dsc.AppendLine(Lang.Get("Temperature: {0}°C", GetGlassTemperature(world, inSlot.Itemstack).ToString("0")));

				bool showHeader = true;
				foreach(var item in Utils.GetShardsList(world, code, amount))
				{
					if(showHeader)
					{
						dsc.AppendLine();
						dsc.AppendLine(Lang.Get("glassmaking:Break down to receive:"));
						showHeader = false;
					}
					dsc.AppendFormat("• {0}x {1}", item.StackSize, item.GetName()).AppendLine();
				}
			}
		}

		public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
		{
			bool preventDefault = false;

			foreach(CollectibleBehavior behavior in CollectibleBehaviors)
			{
				EnumHandling handled = EnumHandling.PassThrough;

				behavior.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling, ref handled);

				if(handled != EnumHandling.PassThrough) preventDefault = true;
				if(handled == EnumHandling.PreventSubsequent) return;
			}

			if(preventDefault) return;

			if(firstEvent && blockSel != null)
			{
				var be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
				if(be != null)
				{
					var itemstack = slot.Itemstack;

					var source = be as IGlassmeltSource;
					if(source != null && source.CanInteract(byEntity, blockSel))
					{
						int amount = source.GetGlassAmount();
						bool isTooCold = false;
						if(amount > amountThreshold && CanTakeGlass(byEntity.World, itemstack, source.GetGlassCode(), out isTooCold))
						{
							handling = EnumHandHandling.PreventDefault;
							if(api.Side == EnumAppSide.Client)
							{
								slot.Itemstack.TempAttributes.SetBool("glassmaking:glassFlag", true);
							}
							else
							{
								slot.Itemstack.TempAttributes.RemoveAttribute("glassmaking:glassFlag");
							}
							return;
						}
						else if(isTooCold && api.Side == EnumAppSide.Client)
						{
							((ICoreClientAPI)api).TriggerIngameError(this, "toocold", Lang.Get("glassmaking:The glass melt has cooled down"));
						}
					}
				}
			}
		}

		public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
		{
			bool result = true;
			bool preventDefault = false;

			foreach(CollectibleBehavior behavior in CollectibleBehaviors)
			{
				EnumHandling handled = EnumHandling.PassThrough;

				bool behaviorResult = behavior.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handled);

				if(handled != EnumHandling.PassThrough)
				{
					result &= behaviorResult;
					preventDefault = true;
				}
				if(handled == EnumHandling.PreventSubsequent) return result;
			}
			if(preventDefault) return result;

			if(blockSel == null) return false;
			var be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
			if(be != null)
			{
				var itemstack = slot.Itemstack;

				var source = be as IGlassmeltSource;
				if(source != null && source.CanInteract(byEntity, blockSel))
				{
					// On the client side it means that the animation is started, on the server side it means that the glass is taken
					bool glassFlag = slot.Itemstack.TempAttributes.HasAttribute("glassmaking:glassFlag");

					int amount = source.GetGlassAmount();
					if(glassFlag || amount > amountThreshold && CanTakeGlass(byEntity.World, itemstack, source.GetGlassCode(), out _))
					{
						const float useTime = 3f;
						if(api.Side == EnumAppSide.Client)
						{
							ModelTransform modelTransform = new ModelTransform();
							modelTransform.EnsureDefaultValues();
							modelTransform.Origin.Set(0.5f, 0.2f, 0.5f);

							float offset = AnimUtil.Quad(0, 0.5f, 0.5f, 0, 0.5f / useTime, 2.5f / useTime, Math.Min(secondsUsed / useTime, 1));
							modelTransform.Translation.Set(offset * 2, offset, offset);

							modelTransform.Scale = AnimUtil.Quad(1, 0.9f, 0.9f, 1, 0.5f / useTime, 2.5f / useTime, Math.Min(secondsUsed / useTime, 1));
							modelTransform.Rotation.X = AnimUtil.Tri(0, -10, 0, 1f / (useTime * 3f), Math.Min(secondsUsed / useTime, 1));
							modelTransform.Rotation.Y = AnimUtil.Quad(0, 15, 10, 0, 2f / useTime, 2.5f / useTime, Math.Min(secondsUsed / useTime, 1));
							modelTransform.Rotation.Z = AnimUtil.Tri(0, -90, 0, 0.2f, Math.Min(secondsUsed / 2, 1));
							byEntity.Controls.UsingHeldItemTransformBefore = modelTransform;
						}
						const float addTime = 1.5f;
						if(api.Side == EnumAppSide.Server && secondsUsed >= addTime)
						{
							if(!glassFlag)
							{
								slot.Itemstack.TempAttributes.SetBool("glassmaking:glassFlag", true);

								AddGlass(byEntity, slot, amount - amountThreshold, source.GetGlassCode(), source.GetTemperature(), out int consumed);
								source.RemoveGlass(consumed);
								slot.MarkDirty();
							}
							if(secondsUsed >= useTime)
							{
								return false;
							}
						}
						if(secondsUsed > 1f)
						{
							IPlayer byPlayer = null;
							if(byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
							source.SpawnMeltParticles(byEntity.World, blockSel, byPlayer);
						}
						return true;
					}
				}
			}
			return false;
		}

		public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
		{
			base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
			slot.Itemstack.TempAttributes.RemoveAttribute("glassmaking:glassFlag");
		}

		public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
		{
			slot.Itemstack.TempAttributes.RemoveAttribute("glassmaking:glassFlag");
			return true;
		}

		public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
		{
			var glassmelt = itemstack.Attributes.GetTreeAttribute("glassmelt");
			if(glassmelt != null)
			{
				var temperature = GlassRenderUtil.TemperatureToState(GetGlassTemperature(capi.World, itemstack), GetWorkingTemperature(itemstack));
				mod.itemsRenderer.RenderItem<GlassLadleRenderer, GlassLadleRenderer.Data>(capi, itemstack, new GlassLadleRenderer.Data(temperature, glassmelt), ref renderinfo);
				return;
			}

			mod.itemsRenderer.RemoveRenderer(itemstack);
			base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
		}

		public override void OnConsumedByCrafting(ItemSlot[] allInputSlots, ItemSlot stackInSlot, GridRecipe gridRecipe, CraftingRecipeIngredient fromIngredient, IPlayer byPlayer, int quantity)
		{
			if(gridRecipe.Output.ResolvedItemstack?.Item is ItemGlassLadle && gridRecipe.Attributes?.IsTrue("breakglass") == true)
			{
				if(api.Side == EnumAppSide.Server)
				{
					var glassmelt = stackInSlot.Itemstack.Attributes.GetTreeAttribute("glassmelt");
					if(glassmelt != null)
					{
						var entity = byPlayer.Entity;
						foreach(var item in Utils.GetShardsList(api.World, new AssetLocation(glassmelt.GetString("code")), glassmelt.GetInt("amount")))
						{
							if(!entity.TryGiveItemStack(item))
							{
								entity.World.SpawnItemEntity(item, byPlayer.Entity.Pos.XYZ.Add(0.0, 0.5, 0.0));
							}
						}
					}
				}
			}
			base.OnConsumedByCrafting(allInputSlots, stackInSlot, gridRecipe, fromIngredient, byPlayer, quantity);
		}

		public override bool MatchesForCrafting(ItemStack inputStack, GridRecipe gridRecipe, CraftingRecipeIngredient ingredient)
		{
			if(gridRecipe.Output.ResolvedItemstack?.Item is ItemGlassLadle && ingredient.ResolvedItemstack?.Item is ItemGlassLadle &&
				gridRecipe.Attributes?.IsTrue("breakglass") == true)
			{
				return inputStack.Attributes.HasAttribute("glassmelt");
			}
			return base.MatchesForCrafting(inputStack, gridRecipe, ingredient);
		}

		public bool IsWorkingTemperature(IWorldAccessor world, ItemStack item)
		{
			return GetGlassTemperature(world, item) >= GetWorkingTemperature(item);
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

		private float GetWorkingTemperature(ItemStack item)
		{
			var glassmelt = item.Attributes.GetTreeAttribute("glassmelt");
			if(glassmelt == null) return 0;

			return mod.GetGlassTypeInfo(new AssetLocation(glassmelt.GetString("code"))).meltingPoint;
		}

		private bool CanTakeGlass(IWorldAccessor world, ItemStack itemStack, AssetLocation code, out bool isTooCold)
		{
			isTooCold = false;
			var glassmelt = itemStack.Attributes.GetTreeAttribute("glassmelt");
			if(glassmelt == null) return true;

			if(glassmelt.GetInt("amount") >= maxGlassAmount || glassmelt.GetString("code") != code.ToShortString())
			{
				return false;
			}

			if(IsWorkingTemperature(world, itemStack))
			{
				return true;
			}
			else
			{
				isTooCold = true;
				return false;
			}
		}

		private void AddGlass(EntityAgent byEntity, ItemSlot slot, int amount, AssetLocation code, float temperature, out int consumed)
		{
			var glassmelt = slot.Itemstack.Attributes.GetOrAddTreeAttribute("glassmelt");

			int currentAmount = glassmelt.GetInt("amount", 0);
			string glassCode = code.ToShortString();
			consumed = Math.Min(maxGlassAmount - currentAmount, amount);
			if(currentAmount > 0)
			{
				glassmelt.SetInt("amount", currentAmount + consumed);
			}
			else
			{
				glassmelt.SetString("code", glassCode);
				glassmelt.SetInt("amount", consumed);
			}

			ChangeGlassTemperature(byEntity.World, slot.Itemstack, currentAmount + consumed, consumed, temperature);
		}
	}
}