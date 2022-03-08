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
		private GlassMakingMod mod;
		private int maxGlassAmount;
		private ModelTransform glassTransform;
		private WorldInteraction[] interactions;

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
			mod = api.ModLoader.GetModSystem<GlassMakingMod>();
			maxGlassAmount = Attributes["maxGlass"].AsInt();
			glassTransform = Attributes["glassTransform"].AsObject<ModelTransform>();
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

					//var mold = be as IEntityGlassBlowingMold;
					//if(mold != null)
					//{
					//	var glassmelt = itemstack.Attributes.GetTreeAttribute("glassmelt");
					//	if(glassmelt != null)
					//	{
					//		if(IsWorkingTemperature(byEntity.World, itemstack))
					//		{
					//			var codesAttrib = glassmelt["code"] as StringArrayAttribute;
					//			var amountsAttrib = glassmelt["amount"] as IntArrayAttribute;
					//			if(mold.CanReceiveGlass(codesAttrib.value, amountsAttrib.value, out _))
					//			{
					//				byEntity.World.RegisterCallback((world, pos, dt) => {
					//					if(byEntity.Controls.HandUse == EnumHandInteract.HeldItemInteract)
					//					{
					//						IPlayer dualCallByPlayer = null;
					//						if(byEntity is EntityPlayer)
					//						{
					//							dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
					//						}
					//						world.PlaySoundAt(new AssetLocation("sounds/sizzle"), byEntity, dualCallByPlayer);
					//					}
					//				}, blockSel.Position, 666);
					//				handling = EnumHandHandling.PreventDefault;
					//				return;
					//			}
					//		}
					//		else if(api.Side == EnumAppSide.Client)
					//		{
					//			((ICoreClientAPI)api).TriggerIngameError(this, "toocold", Lang.Get("glassmaking:The workpiece is not hot enough to work"));
					//		}
					//	}
					//}
					var source = be as IGlassmeltSource;
					if(source != null && source.CanInteract(byEntity, blockSel))
					{
						int amount = source.GetGlassAmount();
						bool isTooCold = false;
						if(amount > 0 && CanTakeGlass(byEntity.World, itemstack, out isTooCold))
						{
							itemstack.TempAttributes.SetFloat("glassmaking:lastAddGlassTime", 0f);
							handling = EnumHandHandling.PreventDefault;
							return;
						}
						else if(isTooCold)
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

				//var mold = be as IEntityGlassBlowingMold;
				//if(mold != null)
				//{
				//	var glassmelt = itemstack.Attributes.GetTreeAttribute("glassmelt");
				//	if(glassmelt != null)
				//	{
				//		if(IsWorkingTemperature(byEntity.World, itemstack))
				//		{
				//			var codesAttrib = glassmelt["code"] as StringArrayAttribute;
				//			var amountsAttrib = glassmelt["amount"] as IntArrayAttribute;
				//			if(mold.CanReceiveGlass(codesAttrib.value, amountsAttrib.value, out float fillTime))
				//			{
				//				const float speed = 1.5f;
				//				if(api.Side == EnumAppSide.Client)
				//				{
				//					ModelTransform modelTransform = new ModelTransform();
				//					modelTransform.EnsureDefaultValues();
				//					modelTransform.Origin.Z = 0;
				//					modelTransform.Translation.Set(-Math.Min(1.275f, speed * secondsUsed * 1.5f), -Math.Min(0.5f, speed * secondsUsed), -Math.Min(0.25f, speed * Math.Max(0, secondsUsed - 0.5f) * 0.5f));
				//					modelTransform.Scale = 1f + Math.Min(0.25f, speed * secondsUsed / 4f);
				//					modelTransform.Rotation.X = -Math.Min(25f, secondsUsed * 45f * speed);
				//					byEntity.Controls.UsingHeldItemTransformBefore = modelTransform;
				//				}
				//				if(api.Side == EnumAppSide.Server && secondsUsed >= 1f + fillTime)
				//				{
				//					mold.TakeGlass(byEntity, codesAttrib.value, amountsAttrib.value);
				//					Dictionary<string, int> shards = new Dictionary<string, int>();
				//					for(int i = 0; i < codesAttrib.value.Length; i++)
				//					{
				//						if(amountsAttrib.value[i] > 0)
				//						{
				//							int count;
				//							if(!shards.TryGetValue(codesAttrib.value[i], out count)) count = 0;
				//							shards[codesAttrib.value[i]] = count + amountsAttrib.value[i];
				//						}
				//					}
				//					itemstack.Attributes.RemoveAttribute("glassmelt");
				//					slot.MarkDirty();

				//					foreach(var item in Utils.GetShardsList(api.World, shards))
				//					{
				//						if(!byEntity.TryGiveItemStack(item))
				//						{
				//							byEntity.World.SpawnItemEntity(item, byEntity.Pos.XYZ.Add(0.0, 0.5, 0.0));
				//						}
				//					}
				//					return false;
				//				}
				//				if(secondsUsed > 1f / speed)
				//				{
				//					IPlayer byPlayer = null;
				//					if(byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
				//					// Smoke on the mold
				//					Vec3d blockpos = blockSel.Position.ToVec3d().Add(0.5, 0.2, 0.5);
				//					float y2 = 0;
				//					Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
				//					Cuboidf[] collboxs = block.GetCollisionBoxes(byEntity.World.BlockAccessor, blockSel.Position);
				//					for(int i = 0; collboxs != null && i < collboxs.Length; i++)
				//					{
				//						y2 = Math.Max(y2, collboxs[i].Y2);
				//					}
				//					byEntity.World.SpawnParticles(
				//						Math.Max(1, 12 - (secondsUsed - 1) * 6),
				//						ColorUtil.ToRgba(50, 220, 220, 220),
				//						blockpos.AddCopy(-0.5, y2 - 2 / 16f, -0.5),
				//						blockpos.Add(0.5, y2 - 2 / 16f + 0.15, 0.5),
				//						new Vec3f(-0.5f, 0f, -0.5f),
				//						new Vec3f(0.5f, 0f, 0.5f),
				//						1.5f,
				//						-0.05f,
				//						0.75f,
				//						EnumParticleModel.Quad,
				//						byPlayer
				//					);
				//				}
				//				return true;
				//			}
				//		}
				//	}
				//}
				var source = be as IGlassmeltSource;
				if(source != null && source.CanInteract(byEntity, blockSel))
				{
					if(itemstack.TempAttributes.HasAttribute("glassmaking:lastAddGlassTime"))
					{
						int amount = source.GetGlassAmount();
						if(amount > 0 && CanTakeGlass(byEntity.World, itemstack, out _))
						{
							const float speed = 1.5f;
							if(api.Side == EnumAppSide.Client)
							{
								ModelTransform modelTransform = new ModelTransform();
								modelTransform.EnsureDefaultValues();
								modelTransform.Origin.Set(0f, 0f, 0f);
								modelTransform.Translation.Set(-Math.Min(0.5f, speed * secondsUsed), -Math.Min(0.5f, speed * secondsUsed), Math.Min(0.5f, speed * secondsUsed));
								modelTransform.Scale = 1f - Math.Min(0.1f, speed * secondsUsed / 4f);
								modelTransform.Rotation.X = -Math.Min(10f, secondsUsed * 45f * speed);
								modelTransform.Rotation.Y = -Math.Min(15f, secondsUsed * 45f * speed) + GameMath.FastSin(secondsUsed * 1.5f);
								modelTransform.Rotation.Z = secondsUsed * 90f % 360f;
								byEntity.Controls.UsingHeldItemTransformBefore = modelTransform;
							}
							const float useTime = 2f;
							if(api.Side == EnumAppSide.Server && secondsUsed >= useTime)
							{
								if(itemstack.TempAttributes.GetFloat("glassmaking:lastAddGlassTime") + useTime <= secondsUsed)
								{
									itemstack.TempAttributes.SetFloat("glassmaking:lastAddGlassTime", (float)Math.Floor(secondsUsed));
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
			}
			return false;
		}

		public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
		{
			base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
			slot.Itemstack.TempAttributes.RemoveAttribute("glassmaking:lastAddGlassTime");
		}

		public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
		{
			slot.Itemstack.TempAttributes.RemoveAttribute("glassmaking:lastAddGlassTime");
			return true;
		}

		public bool IsWorkingTemperature(IWorldAccessor world, ItemStack item)
		{
			return GetGlassTemperature(world, item) >= GetWorkingTemperature(item);
		}

		private float GetWorkingTemperature(ItemStack item)
		{
			var glassmelt = item.Attributes.GetTreeAttribute("glassmelt");
			if(glassmelt == null) return 0;

			return mod.GetGlassTypeInfo(new AssetLocation(glassmelt.GetString("code"))).meltingPoint;
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

		private bool CanTakeGlass(IWorldAccessor world, ItemStack itemStack, out bool isTooCold)
		{
			isTooCold = false;
			var glassmelt = itemStack.Attributes.GetTreeAttribute("glassmelt");
			if(glassmelt == null) return true;

			if(glassmelt.GetInt("amount") >= maxGlassAmount)
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

		private void AddGlass(EntityAgent byEntity, ItemSlot slot, int amount, AssetLocation code, int multiplier, float temperature, out int consumed)
		{
			var glassmelt = slot.Itemstack.Attributes.GetOrAddTreeAttribute("glassmelt");

			int currentAmount = glassmelt.GetInt("amount", 0);
			string glassCode = code.ToShortString();
			consumed = Math.Min(maxGlassAmount - currentAmount, Math.Min(amount, multiplier * (5 + (int)(currentAmount * 0.01f))));
			if(currentAmount > 0 && glassmelt.GetString("code") == glassCode)
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