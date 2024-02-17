using GlassMaking.Blocks;
using GlassMaking.Common;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace GlassMaking.Items.Behavior
{
	public class GlasspipeMoldBehavior : GlasspipeCraftBehavior, ICraftingGridBehavior, IRenderBehavior
	{
		private const string ATTRIB_KEY = "glasslayers";
		private const string ADDTIME_ATTRIB = "glassmaking:lastAddGlassTime";

		public override double Priority => 0.5;

		private int maxGlassAmount;
		private string blowAnimation;
		private string intakeAnimation;
		private GlassMakingMod mod;

		public GlasspipeMoldBehavior(CollectibleObject collObj) : base(collObj)
		{
		}

		public override void Initialize(JsonObject properties)
		{
			base.Initialize(properties);
			blowAnimation = properties["blowAnimation"].AsString();
			intakeAnimation = properties["intakeAnimation"].AsString();
		}

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
			mod = api.ModLoader.GetModSystem<GlassMakingMod>();
			maxGlassAmount = glassworkPipe.Attributes["maxGlass"].AsInt();
		}

		public override bool IsActive(ItemStack itemStack)
		{
			return itemStack.Attributes.HasAttribute(ATTRIB_KEY);
		}

		public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
		{
			if(glassworkPipe.GetActiveCraft(inSlot.Itemstack) == null)
			{
				var sources = Utils.GetGlassmeltSources(api);
				return new WorldInteraction[] {
					new WorldInteraction() {
						ActionLangCode = "glassmaking:heldhelp-glasspipe-intake",
						MouseButton = EnumMouseButton.Right,
						Itemstacks = sources
					},
					new WorldInteraction() {
						ActionLangCode = "glassmaking:heldhelp-glasspipe-intake",
						MouseButton = EnumMouseButton.Right,
						HotKeyCode = "sneak",
						Itemstacks = sources
					}
				};
			}
			return Array.Empty<WorldInteraction>();
		}

		public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
			base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
			var itemstack = inSlot.Itemstack;
			var glasslayers = itemstack.Attributes.GetTreeAttribute(ATTRIB_KEY);
			if(glasslayers != null)
			{
				dsc.AppendLine(Lang.Get("glassmaking:Layers:"));
				var codes = ((StringArrayAttribute)glasslayers["code"]).value;
				var amounts = ((IntArrayAttribute)glasslayers["amount"]).value;
				var amountByCode = new Dictionary<string, int>();
				for(int i = 0; i < codes.Length; i++)
				{
					dsc.AppendFormat("• {0}x {1}", amounts[i], Lang.Get(GlassBlend.GetBlendNameCode(new AssetLocation(codes[i])))).AppendLine();

					if(!amountByCode.TryGetValue(codes[i], out var amount)) amount = 0;
					amountByCode[codes[i]] = amount + amounts[i];
				}

				dsc.AppendLine(Lang.Get("Temperature: {0}°C", glassworkPipe.GetGlassTemperature(world, itemstack).ToString("0")));

				bool showHeader = true;
				foreach(var item in mod.GetShardsList(world, amountByCode))
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

		public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
		{
			if(firstEvent && blockSel != null)
			{
				var itemstack = slot.Itemstack;
				if((glassworkPipe.GetActiveCraft(itemstack) ?? this) != this)
				{
					return;
				}
				var be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
				if(be != null)
				{
					if(be is IEntityGlassBlowingMold mold)
					{
						if(StartMoldInteraction(byEntity, blockSel, itemstack, mold))
						{
							handHandling = EnumHandHandling.PreventDefault;
							handling = EnumHandling.PreventSubsequent;
							return;
						}
					}

					if(be is IGlassmeltSource source && !byEntity.Controls.Sprint)
					{
						if(StartSourceInteraction(byEntity, blockSel, itemstack, source))
						{
							handHandling = EnumHandHandling.PreventDefault;
							handling = EnumHandling.PreventSubsequent;
							return;
						}
					}
				}
			}
		}

		public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
		{
			if(blockSel == null) return false;
			var itemstack = slot.Itemstack;
			if((glassworkPipe.GetActiveCraft(itemstack) ?? this) != this)
			{
				return false;
			}

			var be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
			if(be != null)
			{
				if(be is IEntityGlassBlowingMold mold)
				{
					var result = StepMoldInteraction(slot, byEntity, blockSel, mold, secondsUsed);
					if(result.HasValue)
					{
						handling = EnumHandling.PreventSubsequent;
						return result.Value;
					}
				}

				if(be is IGlassmeltSource source)
				{
					var result = StepSourceInteraction(slot, byEntity, blockSel, source, secondsUsed);
					if(result.HasValue)
					{
						handling = EnumHandling.PreventSubsequent;
						return result.Value;
					}
				}
			}
			return false;
		}

		public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
		{
			var itemstack = slot.Itemstack;
			byEntity?.AnimManager.StopAnimation(blowAnimation);
			if(itemstack.TempAttributes.HasAttribute(ADDTIME_ATTRIB))
			{
				handling = EnumHandling.PreventSubsequent;
				itemstack.TempAttributes.RemoveAttribute(ADDTIME_ATTRIB);
				byEntity?.AnimManager.StopAnimation(intakeAnimation);
			}
		}

		public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason, ref EnumHandling handling)
		{
			var itemstack = slot.Itemstack;
			byEntity?.AnimManager.StopAnimation(blowAnimation);
			if(itemstack.TempAttributes.HasAttribute(ADDTIME_ATTRIB))
			{
				handling = EnumHandling.PreventSubsequent;
				itemstack.TempAttributes.RemoveAttribute(ADDTIME_ATTRIB);
				byEntity?.AnimManager.StopAnimation(intakeAnimation);
			}
			return true;
		}

		public void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo, ref EnumHandling handling)
		{
			var glasslayers = itemstack.Attributes.GetTreeAttribute(ATTRIB_KEY);
			if(glasslayers != null)
			{
				var temperature = GlassRenderUtil.TemperatureToState(glassworkPipe.GetGlassTemperature(capi.World, itemstack), GetWorkingTemperature(capi.World, itemstack));
				glassMaking.itemsRenderer.RenderItem<PipeLayersRenderer, PipeLayersRenderer.Data>(
					capi,
					itemstack,
					new PipeLayersRenderer.Data(temperature, glasslayers),
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
					var itemstack = stackInSlot.Itemstack;
					var glasslayers = itemstack.Attributes.GetTreeAttribute(ATTRIB_KEY);
					if(glasslayers != null)
					{
						handling = EnumHandling.Handled;

						var codes = ((StringArrayAttribute)glasslayers["code"]).value;
						var amounts = ((IntArrayAttribute)glasslayers["amount"]).value;
						var amountByCode = new Dictionary<string, int>();
						for(int i = 0; i < codes.Length; i++)
						{
							if(!amountByCode.TryGetValue(codes[i], out var amount)) amount = 0;
							amountByCode[codes[i]] = amount + amounts[i];
						}

						var entity = byPlayer?.Entity;
						if(entity != null)
						{
							foreach(var item in mod.GetShardsList(api.World, amountByCode))
							{
								if(!entity.TryGiveItemStack(item))
								{
									entity.World.SpawnItemEntity(item, byPlayer.Entity.Pos.XYZ.Add(0.0, 0.5, 0.0));
								}
							}
						}
					}
				}
			}
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
			var glasslayers = itemStack.Attributes.GetTreeAttribute(ATTRIB_KEY);
			if(glasslayers == null) return 0;

			var codesAttrib = glasslayers["code"] as StringArrayAttribute;
			if(codesAttrib.value.Length == 0) return 0;

			float point = 0f;
			foreach(var code in codesAttrib.value)
			{
				point += glassMaking.GetGlassTypeInfo(new AssetLocation(code)).meltingPoint;
			}
			return point / codesAttrib.value.Length * 0.8f;
		}

		private bool StartMoldInteraction(EntityAgent byEntity, BlockSelection blockSel, ItemStack itemstack, IEntityGlassBlowingMold mold)
		{
			var glasslayers = itemstack.Attributes.GetTreeAttribute(ATTRIB_KEY);
			if(glasslayers != null)
			{
				if(IsWorkingTemperature(byEntity.World, itemstack))
				{
					var codesAttrib = glasslayers["code"] as StringArrayAttribute;
					var amountsAttrib = glasslayers["amount"] as IntArrayAttribute;
					if(mold.CanReceiveGlass(codesAttrib.value, amountsAttrib.value, out _))
					{
						byEntity.AnimManager.StartAnimation(blowAnimation);
						byEntity.World.RegisterCallback((world, pos, dt) => {
							if(byEntity.Controls.HandUse == EnumHandInteract.HeldItemInteract)
							{
								IPlayer dualCallByPlayer = null;
								if(byEntity is EntityPlayer)
								{
									dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
								}
								world.PlaySoundAt(new AssetLocation("sounds/sizzle"), byEntity, dualCallByPlayer);
							}
						}, blockSel.Position, 666);
					}
				}
				else if(api.Side == EnumAppSide.Client)
				{
					((ICoreClientAPI)api).TriggerIngameError(this, "toocold", Lang.Get("glassmaking:The workpiece is not hot enough to work"));
				}
				return true;
			}
			return false;
		}

		private bool? StepMoldInteraction(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, IEntityGlassBlowingMold mold, float secondsUsed)
		{
			var itemstack = slot.Itemstack;
			var glasslayers = itemstack.Attributes.GetTreeAttribute(ATTRIB_KEY);
			if(glasslayers != null)
			{
				if(IsWorkingTemperature(byEntity.World, itemstack))
				{
					var codesAttrib = glasslayers["code"] as StringArrayAttribute;
					var amountsAttrib = glasslayers["amount"] as IntArrayAttribute;
					if(mold.CanReceiveGlass(codesAttrib.value, amountsAttrib.value, out float fillTime))
					{
						const float speed = 1.5f;
						if(api.Side == EnumAppSide.Server && secondsUsed >= 1f + fillTime)
						{
							mold.TakeGlass(byEntity, codesAttrib.value, amountsAttrib.value);
							Dictionary<string, int> shards = new Dictionary<string, int>();
							for(int i = 0; i < codesAttrib.value.Length; i++)
							{
								if(amountsAttrib.value[i] > 0)
								{
									int count;
									if(!shards.TryGetValue(codesAttrib.value[i], out count)) count = 0;
									shards[codesAttrib.value[i]] = count + amountsAttrib.value[i];
								}
							}
							itemstack.Attributes.RemoveAttribute(ATTRIB_KEY);
							glassworkPipe.ResetGlassTemperature(api.World, itemstack);
							slot.MarkDirty();

							foreach(var item in mod.GetShardsList(api.World, shards))
							{
								if(!byEntity.TryGiveItemStack(item))
								{
									byEntity.World.SpawnItemEntity(item, byEntity.Pos.XYZ.Add(0.0, 0.5, 0.0));
								}
							}
							return false;
						}
						if(secondsUsed > 1f / speed)
						{
							IPlayer byPlayer = null;
							if(byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
							// Smoke on the mold
							Vec3d blockpos = blockSel.Position.ToVec3d().Add(0.5, 0.2, 0.5);
							float y2 = 0;
							Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
							Cuboidf[] collboxs = block.GetCollisionBoxes(byEntity.World.BlockAccessor, blockSel.Position);
							for(int i = 0; collboxs != null && i < collboxs.Length; i++)
							{
								y2 = Math.Max(y2, collboxs[i].Y2);
							}
							byEntity.World.SpawnParticles(
								Math.Max(1, 12 - (secondsUsed - 1) * 6),
								ColorUtil.ToRgba(50, 220, 220, 220),
								blockpos.AddCopy(-0.5, y2 - 2 / 16f, -0.5),
								blockpos.Add(0.5, y2 - 2 / 16f + 0.15, 0.5),
								new Vec3f(-0.5f, 0f, -0.5f),
								new Vec3f(0.5f, 0f, 0.5f),
								1.5f,
								-0.05f,
								0.75f,
								EnumParticleModel.Quad,
								byPlayer
							);
						}
						return true;
					}
				}
			}
			return null;
		}

		private bool StartSourceInteraction(EntityAgent byEntity, BlockSelection blockSel, ItemStack itemstack, IGlassmeltSource source)
		{
			if(source.CanInteract(byEntity, blockSel))
			{
				int amount = source.GetGlassAmount();
				bool isTooCold = false;
				if(amount > 0 && CanTakeGlass(byEntity.World, itemstack, out isTooCold))
				{
					byEntity.AnimManager.StartAnimation(intakeAnimation);
					itemstack.TempAttributes.SetFloat(ADDTIME_ATTRIB, 0f);
				}
				else if(isTooCold && api.Side == EnumAppSide.Client)
				{
					((ICoreClientAPI)api).TriggerIngameError(this, "toocold", Lang.Get("glassmaking:The workpiece is not hot enough to work"));
				}
				return true;
			}
			return false;
		}

		private bool? StepSourceInteraction(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, IGlassmeltSource source, float secondsUsed)
		{
			if(source.CanInteract(byEntity, blockSel))
			{
				var itemstack = slot.Itemstack;
				if(itemstack.TempAttributes.HasAttribute(ADDTIME_ATTRIB))
				{
					int amount = source.GetGlassAmount();
					if(amount > 0 && CanTakeGlass(byEntity.World, itemstack, out _))
					{
						const float speed = 1.5f;
						const float useTime = 2f;
						if(api.Side == EnumAppSide.Server && secondsUsed >= useTime)
						{
							if(itemstack.TempAttributes.GetFloat(ADDTIME_ATTRIB) + useTime <= secondsUsed)
							{
								itemstack.TempAttributes.SetFloat(ADDTIME_ATTRIB, (float)Math.Floor(secondsUsed));
								AddGlass(byEntity, slot, amount, source.GetGlassCode(), byEntity.Controls.Sneak ? 5 : 1, source.GetTemperature(), out int consumed);
								source.RemoveGlass(consumed);
								slot.MarkDirty();
								return true;
							}
						}
						if(secondsUsed > 1f / speed)
						{
							IPlayer byPlayer = null;
							if(byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
							source.SpawnMeltParticles(byEntity.World, blockSel, byPlayer);
						}
						return true;
					}
				}
			}
			return null;
		}

		private bool CanTakeGlass(IWorldAccessor world, ItemStack itemStack, out bool isTooCold)
		{
			isTooCold = false;
			var glasslayers = itemStack.Attributes.GetTreeAttribute(ATTRIB_KEY);
			if(glasslayers == null) return true;

			var amountsAttrib = glasslayers["amount"] as IntArrayAttribute;

			int count = 0;
			foreach(int amount in amountsAttrib.value)
			{
				count += amount;
			}
			if(count >= maxGlassAmount)
			{
				return false;
			}

			if(IsHeated(world, itemStack))
			{
				return true;
			}
			else
			{
				isTooCold = true;
				return false;
			}
		}

		private void AddGlass(EntityAgent byEntity, ItemSlot slot, int amount, AssetLocation code, int multiplier, float temperature, out int consumed)
		{
			var glasslayers = slot.Itemstack.Attributes.GetOrAddTreeAttribute(ATTRIB_KEY);
			var codesAttrib = glasslayers["code"] as StringArrayAttribute;
			var amountsAttrib = glasslayers["amount"] as IntArrayAttribute;
			if(codesAttrib == null)
			{
				codesAttrib = new StringArrayAttribute(new string[0]);
				glasslayers["code"] = codesAttrib;
				amountsAttrib = new IntArrayAttribute(new int[0]);
				glasslayers["amount"] = amountsAttrib;
			}

			int currentAmount = 0;
			foreach(var c in amountsAttrib.value)
			{
				currentAmount += c;
			}

			string glassCode = code.ToShortString();
			consumed = Math.Min(maxGlassAmount - currentAmount, Math.Min(amount, multiplier * (5 + (int)(currentAmount * 0.01f))));
			if(codesAttrib.value.Length > 0 && codesAttrib.value[codesAttrib.value.Length - 1] == glassCode)
			{
				amountsAttrib.value[amountsAttrib.value.Length - 1] += consumed;
			}
			else
			{
				codesAttrib.value = codesAttrib.value.Append(glassCode);
				amountsAttrib.value = amountsAttrib.value.Append(consumed);
			}

			glassworkPipe.ChangeGlassTemperature(byEntity.World, slot.Itemstack, currentAmount + consumed, consumed, temperature);
		}
	}
}