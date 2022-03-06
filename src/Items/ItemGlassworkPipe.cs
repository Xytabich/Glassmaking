using GlassMaking.Blocks;
using GlassMaking.Common;
using GlassMaking.GenericItemAction;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace GlassMaking.Items
{
	public class ItemGlassworkPipe : Item, IItemCrafter, IGenericHeldItemAction
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
				interactions = ObjectCacheUtil.GetOrCreate(api, "glassmaking:heldhelp-glasspipe", delegate {
					List<ItemStack> list = new List<ItemStack>();
					var capi = api as ICoreClientAPI;
					foreach(Block block in api.World.Blocks)
					{
						if(block is BlockGlassSmeltery)
						{
							List<ItemStack> stacks = block.GetHandBookStacks(capi);
							if(stacks != null) list.AddRange(stacks);
						}
					}
					return new WorldInteraction[] {
						new WorldInteraction() {
							ActionLangCode = "glassmaking:heldhelp-glasspipe-intake",
							MouseButton = EnumMouseButton.Right,
							Itemstacks = list.ToArray()
						},
						new WorldInteraction() {
							ActionLangCode = "glassmaking:heldhelp-glasspipe-intake",
							MouseButton = EnumMouseButton.Right,
							HotKeyCode = "sneak",
							Itemstacks = list.ToArray()
						},
						new WorldInteraction() {
							ActionLangCode = "glassmaking:heldhelp-glasspipe-heatup",
							MouseButton = EnumMouseButton.Right,
							HotKeyCode = "sprint",
							Itemstacks = list.ToArray()
						}
					};
				});
			}
		}

		public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
			base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
			var itemstack = inSlot.Itemstack;
			var recipeAttribute = itemstack.Attributes.GetTreeAttribute("glassmaking:recipe");
			if(recipeAttribute != null)
			{
				var recipe = mod.GetGlassBlowingRecipe(recipeAttribute.GetString("code"));
				if(recipe != null)
				{
					recipe.GetRecipeInfo(itemstack, recipeAttribute, dsc, world, withDebugInfo);

					dsc.AppendLine(Lang.Get("Temperature: {0}°C", GetGlassTemperature(world, inSlot.Itemstack).ToString("0")));

					List<ItemStack> items = new List<ItemStack>();
					recipe.GetBreakDrops(itemstack, recipeAttribute, world, items);

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
			var glasslayers = itemstack.Attributes.GetTreeAttribute("glasslayers");
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

				dsc.AppendLine(Lang.Get("Temperature: {0}°C", GetGlassTemperature(world, inSlot.Itemstack).ToString("0")));

				bool showHeader = true;
				foreach(var item in Utils.GetShardsList(world, amountByCode))
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

		public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
		{
			var itemstack = inSlot.Itemstack;
			var list = interactions.Append(base.GetHeldInteractionHelp(inSlot));
			if(mod.GetGlassBlowingRecipes().Count > 0 && !itemstack.Attributes.HasAttribute("glasslayers") && !itemstack.Attributes.HasAttribute("glassmaking:recipe"))
			{
				var tmp = new WorldInteraction[] {
					new WorldInteraction() {
						ActionLangCode = "glassmaking:heldhelp-glasspipe-recipe",
						HotKeyCode = "itemrecipeselect",
						MouseButton = EnumMouseButton.None
					}
				}.Append(list);
				list = tmp;
			}
			return list;
		}

		public override void OnConsumedByCrafting(ItemSlot[] allInputSlots, ItemSlot stackInSlot, GridRecipe gridRecipe, CraftingRecipeIngredient fromIngredient, IPlayer byPlayer, int quantity)
		{
			if(gridRecipe.Output.ResolvedItemstack?.Item is ItemGlassworkPipe && gridRecipe.Attributes?.IsTrue("breakglass") == true)
			{
				if(api.Side == EnumAppSide.Server)
				{
					var itemstack = stackInSlot.Itemstack;
					var recipeAttribute = itemstack.Attributes.GetTreeAttribute("glassmaking:recipe");
					if(recipeAttribute != null)
					{
						var recipe = mod.GetGlassBlowingRecipe(recipeAttribute.GetString("code"));
						if(recipe != null)
						{
							var entity = byPlayer.Entity;
							List<ItemStack> items = new List<ItemStack>();
							recipe.GetBreakDrops(itemstack, recipeAttribute, entity.World, items);
							foreach(var item in items)
							{
								item.StackSize *= quantity;
								if(!entity.TryGiveItemStack(item))
								{
									entity.World.SpawnItemEntity(item, byPlayer.Entity.Pos.XYZ.Add(0.0, 0.5, 0.0));
								}
							}
						}
					}
					var glasslayers = itemstack.Attributes.GetTreeAttribute("glasslayers");
					if(glasslayers != null)
					{
						var codes = ((StringArrayAttribute)glasslayers["code"]).value;
						var amounts = ((IntArrayAttribute)glasslayers["amount"]).value;
						var amountByCode = new Dictionary<string, int>();
						for(int i = 0; i < codes.Length; i++)
						{
							if(!amountByCode.TryGetValue(codes[i], out var amount)) amount = 0;
							amountByCode[codes[i]] = amount + amounts[i];
						}

						var entity = byPlayer.Entity;
						foreach(var item in Utils.GetShardsList(api.World, amountByCode))
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
			if(gridRecipe.Output.ResolvedItemstack?.Item is ItemGlassworkPipe && ingredient.ResolvedItemstack?.Item is ItemGlassworkPipe &&
				gridRecipe.Attributes?.IsTrue("breakglass") == true)
			{
				return inputStack.Attributes.HasAttribute("glassmaking:recipe") || inputStack.Attributes.HasAttribute("glasslayers");
			}
			return base.MatchesForCrafting(inputStack, gridRecipe, ingredient);
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
					bool hasRecipe = itemstack.Attributes.HasAttribute("glassmaking:recipe");

					var mold = be as IEntityGlassBlowingMold;
					if(!hasRecipe && mold != null)
					{
						var glasslayers = itemstack.Attributes.GetTreeAttribute("glasslayers");
						if(glasslayers != null)
						{
							if(IsWorkingTemperature(byEntity.World, itemstack))
							{
								var codesAttrib = glasslayers["code"] as StringArrayAttribute;
								var amountsAttrib = glasslayers["amount"] as IntArrayAttribute;
								if(mold.CanReceiveGlass(codesAttrib.value, amountsAttrib.value, out _))
								{
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
									handling = EnumHandHandling.PreventDefault;
									return;
								}
							}
							else if(api.Side == EnumAppSide.Client)
							{
								((ICoreClientAPI)api).TriggerIngameError(this, "toocold", Lang.Get("glassmaking:The workpiece is not hot enough to work"));
							}
						}
					}
					var source = be as IGlassmeltSource;
					if(source != null && source.CanInteract(byEntity, blockSel))
					{
						if(byEntity.Controls.Sprint)
						{
							if(hasRecipe || itemstack.Attributes.HasAttribute("glasslayers"))
							{
								float temperature = GetGlassTemperature(byEntity.World, itemstack);
								float temp = source.GetTemperature();
								if(temp > temperature)
								{
									float minTemperature = GetWorkingTemperature(byEntity.World, slot.Itemstack) * 0.45f;
									if(temperature >= minTemperature)
									{
										slot.Itemstack.TempAttributes.SetFloat("glassmaking:lastHeatTime", 0f);
									}
									else if(api.Side == EnumAppSide.Client)
									{
										((ICoreClientAPI)api).TriggerIngameError(this, "toocold", Lang.Get("glassmaking:Unable to reheat a cold workpiece"));
									}
									handling = EnumHandHandling.PreventDefault;
									return;
								}
							}
						}
						else if(!hasRecipe)
						{
							int amount = source.GetGlassAmount();
							if(amount > 0 && HasFreeSpace(slot.Itemstack))
							{
								slot.Itemstack.TempAttributes.SetFloat("glassmaking:lastAddGlassTime", 0f);
								handling = EnumHandHandling.PreventDefault;
								return;
							}
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
				bool hasRecipe = itemstack.Attributes.HasAttribute("glassmaking:recipe");

				var mold = be as IEntityGlassBlowingMold;
				if(!hasRecipe && mold != null)
				{
					var glasslayers = itemstack.Attributes.GetTreeAttribute("glasslayers");
					if(glasslayers != null)
					{
						if(IsWorkingTemperature(byEntity.World, itemstack))
						{
							var codesAttrib = glasslayers["code"] as StringArrayAttribute;
							var amountsAttrib = glasslayers["amount"] as IntArrayAttribute;
							if(mold.CanReceiveGlass(codesAttrib.value, amountsAttrib.value, out float fillTime))
							{
								const float speed = 1.5f;
								if(api.Side == EnumAppSide.Client)
								{
									ModelTransform modelTransform = new ModelTransform();
									modelTransform.EnsureDefaultValues();
									modelTransform.Origin.Z = 0;
									modelTransform.Translation.Set(-Math.Min(1.275f, speed * secondsUsed * 1.5f), -Math.Min(0.5f, speed * secondsUsed), -Math.Min(0.25f, speed * Math.Max(0, secondsUsed - 0.5f) * 0.5f));
									modelTransform.Scale = 1f + Math.Min(0.25f, speed * secondsUsed / 4f);
									modelTransform.Rotation.X = -Math.Min(25f, secondsUsed * 45f * speed);
									byEntity.Controls.UsingHeldItemTransformBefore = modelTransform;
								}
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
									itemstack.Attributes.RemoveAttribute("glasslayers");
									slot.MarkDirty();

									foreach(var item in Utils.GetShardsList(api.World, shards))
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
				}
				var source = be as IGlassmeltSource;
				if(source != null && source.CanInteract(byEntity, blockSel))
				{
					if(byEntity.Controls.Sprint)
					{
						if(hasRecipe || itemstack.Attributes.HasAttribute("glasslayers"))
						{
							float temperature = GetGlassTemperature(byEntity.World, itemstack);
							float temp = source.GetTemperature();
							if(temp > temperature)
							{
								float minTemperature = GetWorkingTemperature(byEntity.World, slot.Itemstack) * 0.45f;
								if(temperature >= minTemperature)
								{
									if(api.Side == EnumAppSide.Client)
									{
										const float speed = 1.5f;
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
									if(api.Side == EnumAppSide.Server && slot.Itemstack.TempAttributes.GetFloat("glassmaking:lastHeatTime") + 1f <= secondsUsed)
									{
										slot.Itemstack.TempAttributes.SetFloat("glassmaking:lastHeatTime", (float)Math.Floor(secondsUsed));
										SetGlassTemperature(byEntity.World, slot.Itemstack, GameMath.Min(temp, temperature + 100));
										slot.MarkDirty();
									}
									return true;
								}
							}
						}
					}
					else if(!hasRecipe)
					{
						if(slot.Itemstack.TempAttributes.HasAttribute("glassmaking:lastAddGlassTime"))
						{
							int amount = source.GetGlassAmount();
							if(amount > 0 && HasFreeSpace(slot.Itemstack))
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
									if(slot.Itemstack.TempAttributes.GetFloat("glassmaking:lastAddGlassTime") + useTime <= secondsUsed)
									{
										slot.Itemstack.TempAttributes.SetFloat("glassmaking:lastAddGlassTime", (float)Math.Floor(secondsUsed));
										if(amount > 0 && AddGlass(byEntity, slot, amount, source.GetGlassCode(), byEntity.Controls.Sneak ? 5 : 1, source.GetTemperature(), out int consumed))
										{
											source.RemoveGlass(consumed);
											slot.MarkDirty();
											return true;
										}
										else
										{
											return false;
										}
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
			}
			return false;
		}

		public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
		{
			base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
			slot.Itemstack.TempAttributes.RemoveAttribute("glassmaking:lastAddGlassTime");
			slot.Itemstack.TempAttributes.RemoveAttribute("glassmaking:lastHeatTime");
		}

		public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
		{
			slot.Itemstack.TempAttributes.RemoveAttribute("glassmaking:lastAddGlassTime");
			slot.Itemstack.TempAttributes.RemoveAttribute("glassmaking:lastHeatTime");
			return true;
		}

		public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
		{
			base.OnModifiedInInventorySlot(world, slot, extractedStack);
			if(api.Side == EnumAppSide.Client)
			{
				if(slot.Itemstack.Attributes.HasAttribute("glasslayers") || slot.Itemstack.Attributes.HasAttribute("glassmaking:recipe"))
				{
					SetMeshDirty(slot.Itemstack);
				}
				else
				{
					mod.pipeRenderCache.Remove(slot.Itemstack);
				}
			}
		}

		public virtual float GetGlassTemperature(IWorldAccessor world, ItemStack itemstack)
		{
			if(itemstack == null || itemstack.Attributes == null || itemstack.Attributes["glassTemperature"] == null || !(itemstack.Attributes["glassTemperature"] is ITreeAttribute))
			{
				return 20f;
			}
			ITreeAttribute attr = (ITreeAttribute)itemstack.Attributes["glassTemperature"];
			double totalHours = world.Calendar.TotalHours;
			double lastUpdate = attr.GetDouble("temperatureLastUpdate");
			if(totalHours - lastUpdate > 1.0 / 85)
			{
				float temperature = Math.Max(20f, attr.GetFloat("temperature", 20f) - Math.Max(0f, (float)(totalHours - lastUpdate) * 180f));
				SetGlassTemperature(world, itemstack, temperature);
				return temperature;
			}
			return attr.GetFloat("temperature", 20f);
		}

		public virtual void SetGlassTemperature(IWorldAccessor world, ItemStack itemstack, float temperature, bool delayCooldown = false)
		{
			if(itemstack != null)
			{
				ITreeAttribute attr = (ITreeAttribute)itemstack.Attributes["glassTemperature"];
				if(attr == null)
				{
					attr = new TreeAttribute();
					itemstack.Attributes["glassTemperature"] = attr;
				}
				double totalHours = world.Calendar.TotalHours;
				float prevTemperature = attr.GetFloat("temperature");
				if(delayCooldown && prevTemperature < temperature)
				{
					totalHours += 0.5;
				}

				attr.SetDouble("temperatureLastUpdate", totalHours);
				attr.SetFloat("temperature", temperature);

				if(api.Side == EnumAppSide.Client)
				{
					var workTemperature = GetWorkingTemperature(world, itemstack);
					if(GlasspipeRenderCache.TemperatureToState(prevTemperature, workTemperature) != GlasspipeRenderCache.TemperatureToState(temperature, workTemperature))
					{
						SetMeshDirty(itemstack);
					}
				}
			}
		}

		public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
		{
			var glasslayers = itemstack.Attributes.GetTreeAttribute("glasslayers");
			if(glasslayers != null)
			{
				var container = mod.pipeRenderCache.GetOrCreate(itemstack);
				var temperature = GlasspipeRenderCache.TemperatureToState(GetGlassTemperature(capi.World, itemstack), GetWorkingTemperature(capi.World, itemstack));
				if(container.isDirty || !container.hasMesh || container.temperature != temperature)
				{
					container.temperature = temperature;
					container.isDirty = false;
					UpdateGlasslayersMesh(itemstack, glasslayers, GlasspipeRenderCache.StateToGlow(temperature));
				}

				container.UpdateMeshRef(capi, Shape, capi.Tesselator.GetTextureSource(this), glassTransform);
				renderinfo.ModelRef = container.meshRef;
				renderinfo.CullFaces = true;
				return;
			}
			else
			{
				var recipeAttribute = itemstack.Attributes.GetTreeAttribute("glassmaking:recipe");
				if(recipeAttribute != null)
				{
					var container = mod.pipeRenderCache.GetOrCreate(itemstack);
					var temperature = GlasspipeRenderCache.TemperatureToState(GetGlassTemperature(capi.World, itemstack), GetWorkingTemperature(capi.World, itemstack));
					if(container.isDirty || !container.hasMesh || container.temperature != temperature)
					{
						container.temperature = temperature;
						container.isDirty = false;
						UpdateRecipeMesh(itemstack, recipeAttribute, GlasspipeRenderCache.StateToGlow(temperature));
					}

					container.UpdateMeshRef(capi, Shape, capi.Tesselator.GetTextureSource(this), glassTransform);
					renderinfo.ModelRef = container.meshRef;
					renderinfo.CullFaces = true;
					return;
				}
				else
				{
					mod.pipeRenderCache.Remove(itemstack);
				}
			}
			base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
		}

		public bool TryGetRecipeAttribute(ItemStack itemstack, out ITreeAttribute recipeAttribute)
		{
			recipeAttribute = itemstack.Attributes.GetTreeAttribute("glassmaking:recipe");
			return recipeAttribute != null;
		}

		public void OnRecipeUpdated(ItemSlot slot, bool isComplete)
		{
			if(isComplete)
			{
				slot.Itemstack.Attributes.RemoveAttribute("glassmaking:recipe");
				slot.MarkDirty();
			}
			else if(api.Side == EnumAppSide.Client)
			{
				SetMeshDirty(slot.Itemstack);
			}
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

		public bool IsWorkingTemperature(IWorldAccessor world, ItemStack item)
		{
			return GetGlassTemperature(world, item) >= GetWorkingTemperature(world, item);
		}

		bool IItemCrafter.PreventRecipeAssignment(IClientPlayer player, ItemStack item)
		{
			return item.Attributes.HasAttribute("glassmaking:recipe") || item.Attributes.HasAttribute("glasslayers");
		}

		bool IItemCrafter.TryGetRecipeOutputs(IClientPlayer player, ItemStack item, out KeyValuePair<IAttribute, ItemStack>[] recipeOutputs)
		{
			var recipes = mod.GetGlassBlowingRecipes();
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

		bool IGenericHeldItemAction.GenericHeldItemAction(IPlayer player, string action, ITreeAttribute attributes)
		{
			if(action == "recipe")
			{
				var code = attributes.GetString("key");
				if(!string.IsNullOrEmpty(code))
				{
					var recipe = mod.GetGlassBlowingRecipe(code);
					if(recipe != null)
					{
						var slot = player.InventoryManager.ActiveHotbarSlot;
						slot.Itemstack.Attributes.GetOrAddTreeAttribute("glassmaking:recipe").SetString("code", code);
						slot.MarkDirty();
						return true;
					}
				}
			}
			return false;
		}

		private float GetWorkingTemperature(IWorldAccessor world, ItemStack itemStack)
		{
			var recipeAttribute = itemStack.Attributes.GetTreeAttribute("glassmaking:recipe");
			if(recipeAttribute != null)
			{
				var recipe = mod.GetGlassBlowingRecipe(recipeAttribute.GetString("code"));
				if(recipe != null)
				{
					return recipe.GetWorkingTemperature(itemStack, recipeAttribute, world);
				}
				return 0;
			}

			var glasslayers = itemStack.Attributes.GetTreeAttribute("glasslayers");
			if(glasslayers == null) return 0;

			var codesAttrib = glasslayers["code"] as StringArrayAttribute;
			if(codesAttrib.value.Length == 0) return 0;

			float point = 0f;
			foreach(var code in codesAttrib.value)
			{
				point += mod.GetGlassTypeInfo(new AssetLocation(code)).meltingPoint;
			}
			return point / codesAttrib.value.Length * 0.8f;
		}

		private bool HasFreeSpace(ItemStack itemStack)
		{
			var glasslayers = itemStack.Attributes.GetTreeAttribute("glasslayers");
			if(glasslayers != null)
			{
				var amountsAttrib = glasslayers["amount"] as IntArrayAttribute;

				int count = 0;
				foreach(int amount in amountsAttrib.value)
				{
					count += amount;
				}
				if(count >= maxGlassAmount) return false;
			}
			return true;
		}

		private bool AddGlass(EntityAgent byEntity, ItemSlot slot, int amount, AssetLocation code, int multiplier, float temperature, out int consumed)
		{
			var glasslayers = slot.Itemstack.Attributes.GetOrAddTreeAttribute("glasslayers");
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

			ChangeGlassTemperature(byEntity.World, slot.Itemstack, currentAmount + consumed, consumed, temperature);

			return true;
		}

		private void SetMeshDirty(ItemStack item)
		{
			mod.pipeRenderCache.GetOrCreate(item).isDirty = true;
		}

		private void UpdateGlasslayersMesh(ItemStack item, ITreeAttribute glasslayers, int glow)
		{
			int count = 0;
			var amountsAttrib = glasslayers["amount"] as IntArrayAttribute;
			foreach(var c in amountsAttrib.value)
			{
				count += c;
			}

			var container = mod.pipeRenderCache.GetOrCreate(item);
			var info = container._data as MeltMeshInfo;
			if(info == null || info.count != count || info.glow != glow)
			{
				const double invPI = 1.0 / Math.PI;
				container._data = new MeltMeshInfo(count, glow);

				var root = Math.Pow(count * invPI, 1.0 / 3.0);
				var shape = new SmoothRadialShape();
				shape.Segments = GameMath.Max(1, (int)Math.Floor(root)) * 2 + 3;

				float radius = (float)Math.Sqrt(count * invPI / root);
				float length = (float)(root * 1.5);
				shape.Outer = new SmoothRadialShape.ShapePart[] { new SmoothRadialShape.ShapePart() {
					Vertices = new float[][] {
					   new float[] { -3, 0 },
					   new float[] { length * 0.1f, radius  },
					   new float[] { length, radius },
					   new float[] { length, 0 }
					}
				} };
				container.BeginMeshChange();
				SmoothRadialShape.BuildMesh(container._mesh, shape, (m, i, o) => GlasspipeRenderUtil.GenerateRadialVertices(m, i, o, glow), GlasspipeRenderUtil.GenerateRadialFaces);
				container.EndMeshChange();
			}
		}

		private void UpdateRecipeMesh(ItemStack item, ITreeAttribute recipeAttribute, int glow)
		{
			var recipe = mod.GetGlassBlowingRecipe(recipeAttribute.GetString("code"));
			if(recipe != null)
			{
				recipe.UpdateMesh(recipeAttribute, mod.pipeRenderCache.GetOrCreate(item), glow);
			}
		}

		private class MeltMeshInfo
		{
			internal int count;
			internal int glow;

			public MeltMeshInfo(int count, int glow)
			{
				this.count = count;
				this.glow = glow;
			}
		}

		public interface IMeshContainer
		{
			object Data { get; set; }
			MeshData Mesh { get; }

			void BeginMeshChange();

			void EndMeshChange();
		}
	}
}