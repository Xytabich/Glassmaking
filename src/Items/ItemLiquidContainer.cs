using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GlassMaking.Items
{
	public class ItemLiquidContainer : ItemContainer, ILiquidSource, ILiquidSink
	{
		protected float capacityLitresFromAttributes = 10;
		public virtual float CapacityLitres => capacityLitresFromAttributes;
		public virtual int ContainerSlotId => 0;

		public virtual float TransferSizeLitres => 1;

		public virtual bool CanDrinkFrom => Attributes["canDrinkFrom"].AsBool() == true;
		public virtual bool IsTopOpened => Attributes["isTopOpened"].AsBool() == true;
		public virtual bool AllowHeldLiquidTransfer => Attributes["allowHeldLiquidTransfer"].AsBool() == true;

		private Dictionary<string, ItemStack[]> recipeLiquidContents = new Dictionary<string, ItemStack[]>();

		public override void OnHandbookRecipeRender(ICoreClientAPI capi, GridRecipe gridRecipe, ItemSlot dummyslot, double x, double y, double size)
		{
			// 1.16.0: Fugly (but backwards compatible) hack: We temporarily store the ingredient index in an unused field of ItemSlot so that OnHandbookRecipeRender() has access to that number. Proper solution would be to alter the method signature to pass on this value.
			int rindex = dummyslot.BackgroundIcon.ToInt();
			var ingredient = gridRecipe.resolvedIngredients[rindex];

			JsonObject rprops = ingredient.RecipeAttributes;
			if(rprops?.Exists != true || rprops?["requiresContent"].Exists != true) rprops = gridRecipe.Attributes?["liquidContainerProps"];

			if(rprops?.Exists != true)
			{
				base.OnHandbookRecipeRender(capi, gridRecipe, dummyslot, x, y, size);
				return;
			}

			string contentCode = gridRecipe.Attributes["liquidContainerProps"]["requiresContent"]["code"].AsString();
			string contentType = gridRecipe.Attributes["liquidContainerProps"]["requiresContent"]["type"].AsString();
			float litres = gridRecipe.Attributes["liquidContainerProps"]["requiresLitres"].AsFloat();

			string key = contentType + "-" + contentCode;
			ItemStack[] stacks;
			if(!recipeLiquidContents.TryGetValue(key, out stacks))
			{
				if(contentCode.Contains("*"))
				{
					EnumItemClass contentClass = contentType == "block" ? EnumItemClass.Block : EnumItemClass.Item;
					List<ItemStack> lstacks = new List<ItemStack>();
					var loc = AssetLocation.Create(contentCode, Code.Domain);
					foreach(var obj in api.World.Collectibles)
					{
						if(obj.ItemClass == contentClass && WildcardUtil.Match(loc, obj.Code))
						{
							var stack = new ItemStack(obj);
							var props = GetContainableProps(stack);
							if(props == null) continue;
							stack.StackSize = (int)(props.ItemsPerLitre * litres);
							lstacks.Add(stack);
						}
					}
					stacks = lstacks.ToArray();
				}
				else
				{
					recipeLiquidContents[key] = stacks = new ItemStack[1];

					if(contentType == "item") stacks[0] = new ItemStack(capi.World.GetItem(new AssetLocation(contentCode)));
					else stacks[0] = new ItemStack(capi.World.GetBlock(new AssetLocation(contentCode)));

					var props = GetContainableProps(stacks[0]);
					stacks[0].StackSize = (int)(props.ItemsPerLitre * litres);
				}
			}

			ItemStack filledContainerStack = dummyslot.Itemstack.Clone();

			int index = (int)(capi.ElapsedMilliseconds / 1000) % stacks.Length;

			SetContent(filledContainerStack, stacks[index]);

			dummyslot.Itemstack = filledContainerStack;

			capi.Render.RenderItemstackToGui(
				dummyslot,
				x,
				y,
				100, (float)size * 0.58f, ColorUtil.WhiteArgb,
				true, false, true
			);
		}

		public virtual int GetContainerSlotId(ItemStack containerStack)
		{
			return ContainerSlotId;
		}

		#region Interaction help
		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);

			if(Attributes?["capacityLitres"].Exists == true)
			{
				capacityLitresFromAttributes = Attributes["capacityLitres"].AsFloat(10);
			}
			else
			{
				var props = Attributes?["liquidContainerProps"]?.AsObject<LiquidTopOpenContainerProps>(null, Code.Domain);
				if(props != null)
				{
					capacityLitresFromAttributes = props.CapacityLitres;
				}
			}
		}

		public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
		{
			return new WorldInteraction[]
			{
				new WorldInteraction()
				{
					ActionLangCode = "heldhelp-fill",
					MouseButton = EnumMouseButton.Right,
					ShouldApply = (wi, bs, es) => {
						return GetCurrentLitres(inSlot.Itemstack) < CapacityLitres;
					}
				},
				new WorldInteraction()
				{
					ActionLangCode = "heldhelp-empty",
					HotKeyCode = "sprint",
					MouseButton = EnumMouseButton.Right,
					ShouldApply = (wi, bs, es) => {
						return GetCurrentLitres(inSlot.Itemstack) > 0;
					}
				}
			};
		}

		#endregion



		#region Take/Remove Contents

		public bool SetCurrentLitres(ItemStack containerStack, float litres)
		{
			WaterTightContainableProps props = GetContentProps(containerStack);
			if(props == null) return false;

			ItemStack contentStack = GetContent(containerStack);
			contentStack.StackSize = (int)(litres * props.ItemsPerLitre);

			SetContent(containerStack, contentStack);
			return true;
		}

		public float GetCurrentLitres(ItemStack containerStack)
		{
			WaterTightContainableProps props = GetContentProps(containerStack);
			if(props == null) return 0;

			return (float)GetContent(containerStack).StackSize / props.ItemsPerLitre;
		}

		public bool IsFull(ItemStack containerStack)
		{
			return GetCurrentLitres(containerStack) >= CapacityLitres;
		}

		public WaterTightContainableProps GetContentProps(ItemStack containerStack)
		{
			ItemStack stack = GetContent(containerStack);
			return GetContainableProps(stack);
		}

		public static int GetTransferStackSize(ILiquidInterface containerBlock, ItemStack contentStack, IPlayer player = null)
		{
			return GetTransferStackSize(containerBlock, contentStack, player?.Entity?.Controls.Sneak == true);
		}

		public static int GetTransferStackSize(ILiquidInterface containerBlock, ItemStack contentStack, bool maxCapacity)
		{
			if(contentStack == null) return 0;

			float litres = containerBlock.TransferSizeLitres;
			var liqProps = GetContainableProps(contentStack);
			int stacksize = (int)(liqProps.ItemsPerLitre * litres);

			if(maxCapacity)
			{
				stacksize = (int)(containerBlock.CapacityLitres * liqProps.ItemsPerLitre);
			}

			return stacksize;
		}

		public static WaterTightContainableProps GetContainableProps(ItemStack stack)
		{
			try
			{
				JsonObject obj = stack?.ItemAttributes?["waterTightContainerProps"];
				if(obj != null && obj.Exists) return obj.AsObject<WaterTightContainableProps>(null, stack.Collectible.Code.Domain);
				return null;
			}
			catch(Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// Sets the containers contents to given stack
		/// </summary>
		/// <param name="containerStack"></param>
		/// <param name="content"></param>
		public void SetContent(ItemStack containerStack, ItemStack content)
		{
			if(content == null)
			{
				SetContents(containerStack, null);
				return;
			}
			SetContents(containerStack, new ItemStack[] { content });
		}

		/// <summary>
		/// Retrieve the contents of the container stack
		/// </summary>
		/// <param name="world"></param>
		/// <param name="containerStack"></param>
		/// <returns></returns>
		public ItemStack GetContent(ItemStack containerStack)
		{
			ItemStack[] stacks = GetContents(api.World, containerStack);
			int id = GetContainerSlotId(containerStack);
			return (stacks != null && stacks.Length > 0) ? stacks[Math.Min(stacks.Length - 1, id)] : null;
		}

		public override ItemStack CreateItemStackFromJson(ITreeAttribute stackAttr, IWorldAccessor world, string domain)
		{
			var stack = base.CreateItemStackFromJson(stackAttr, world, domain);

			if(stackAttr.HasAttribute("makefull"))
			{
				var props = GetContainableProps(stack);
				stack.StackSize = (int)(CapacityLitres * props.ItemsPerLitre);
			}

			return stack;
		}

		/// <summary>
		/// Tries to take out as much items/liquid as possible and returns it
		/// </summary>
		/// <param name="world"></param>
		/// <param name="containerStack"></param>
		/// <param name="quantityItems"></param>
		/// <returns></returns>
		public ItemStack TryTakeContent(ItemStack containerStack, int quantityItems)
		{
			ItemStack stack = GetContent(containerStack);
			if(stack == null) return null;

			ItemStack takenStack = stack.Clone();
			takenStack.StackSize = quantityItems;

			stack.StackSize -= quantityItems;
			if(stack.StackSize <= 0) SetContent(containerStack, null);
			else SetContent(containerStack, stack);

			return takenStack;
		}

		public ItemStack TryTakeLiquid(ItemStack containerStack, float desiredLitres)
		{
			var props = GetContainableProps(GetContent(containerStack));
			if(props == null) return null;

			return TryTakeContent(containerStack, (int)(desiredLitres * props.ItemsPerLitre));
		}

		#endregion


		#region PutContents

		/// <summary>
		/// Tries to place in items/liquid and returns actually inserted quantity
		/// </summary>
		/// <param name="containerStack"></param>
		/// <param name="liquidStack"></param>
		/// <param name="desiredLitres"></param>
		/// <returns>Amount of moved items (stacksize, not litres)</returns>
		public virtual int TryPutLiquid(ItemStack containerStack, ItemStack liquidStack, float desiredLitres)
		{
			if(liquidStack == null) return 0;

			var props = GetContainableProps(liquidStack);
			if(props == null) return 0;

			int desiredItems = (int)(props.ItemsPerLitre * desiredLitres);
			int availItems = liquidStack.StackSize;

			ItemStack stack = GetContent(containerStack);
			ILiquidSink sink = containerStack.Collectible as ILiquidSink;

			if(stack == null)
			{
				if(!props.Containable) return 0;

				int placeableItems = (int)(sink.CapacityLitres * props.ItemsPerLitre);

				ItemStack placedstack = liquidStack.Clone();
				placedstack.StackSize = GameMath.Min(availItems, desiredItems, placeableItems);
				SetContent(containerStack, placedstack);

				return Math.Min(desiredItems, placeableItems);
			}
			else
			{
				if(!stack.Equals(api.World, liquidStack, GlobalConstants.IgnoredStackAttributes)) return 0;

				float maxItems = sink.CapacityLitres * props.ItemsPerLitre;
				int placeableItems = (int)(maxItems - (float)stack.StackSize);

				stack.StackSize += Math.Min(placeableItems, desiredItems);

				return Math.Min(placeableItems, desiredItems);
			}
		}
		#endregion

		#region Held Interact

		public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
		{
			if(blockSel == null || byEntity.Controls.Sneak)
			{
				if(CanDrinkFrom && GetNutritionProperties(byEntity.World, itemslot.Itemstack, byEntity) != null)
				{
					tryEatBegin(itemslot, byEntity, ref handHandling, "drink", 4);
				}
				else
				{
					base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
				}
				return;
			}

			if(AllowHeldLiquidTransfer)
			{
				IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;

				ItemStack contentStack = GetContent(itemslot.Itemstack);
				WaterTightContainableProps props = contentStack == null ? null : GetContentProps(contentStack);

				Block targetedBlock = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);

				if(!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
				{
					byEntity.World.BlockAccessor.MarkBlockDirty(blockSel.Position.AddCopy(blockSel.Face));
					byPlayer?.InventoryManager.ActiveHotbarSlot?.MarkDirty();
					return;
				}

				if(!TryFillFromBlock(itemslot, byEntity, blockSel.Position))
				{
					BlockLiquidContainerTopOpened targetCntBlock = targetedBlock as BlockLiquidContainerTopOpened;
					if(targetCntBlock != null)
					{
						if(targetCntBlock.TryPutLiquid(blockSel.Position, contentStack, targetCntBlock.CapacityLitres) > 0)
						{
							TryTakeContent(itemslot.Itemstack, 1);
							byEntity.World.PlaySoundAt(props.FillSpillSound, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
						}

					}
					else
					{
						if(byEntity.Controls.Sprint)
						{
							SpillContents(itemslot, byEntity, blockSel);
						}
					}
				}
			}

			if(CanDrinkFrom)
			{
				if(GetNutritionProperties(byEntity.World, itemslot.Itemstack, byEntity) != null)
				{
					tryEatBegin(itemslot, byEntity, ref handHandling, "drink", 4);
					return;
				}
			}

			if(AllowHeldLiquidTransfer || CanDrinkFrom)
			{
				// Prevent placing on normal use
				handHandling = EnumHandHandling.PreventDefaultAction;
			}
		}

		protected override bool tryEatStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, ItemStack spawnParticleStack = null)
		{
			return base.tryEatStep(secondsUsed, slot, byEntity, GetContent(slot.Itemstack));
		}

		protected override void tryEatStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
		{
			FoodNutritionProperties nutriProps = GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity);

			if(byEntity.World is IServerWorldAccessor && nutriProps != null && secondsUsed >= 0.95f)
			{
				float litres = GetCurrentLitres(slot.Itemstack);
				float litresToDrink = Math.Min(1, litres);
				if(litres > 1)
				{
					nutriProps.Satiety /= litres;
					nutriProps.Health /= litres;
				}

				TransitionState state = UpdateAndGetTransitionState(api.World, slot, EnumTransitionType.Perish);
				float spoilState = state != null ? state.TransitionLevel : 0;

				float satLossMul = GlobalConstants.FoodSpoilageSatLossMul(spoilState, slot.Itemstack, byEntity);
				float healthLossMul = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, slot.Itemstack, byEntity);

				byEntity.ReceiveSaturation(nutriProps.Satiety * satLossMul, nutriProps.FoodCategory);

				IPlayer player = null;
				if(byEntity is EntityPlayer) player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

				splitStackAndPerformAction(byEntity, slot, (stack) => TryTakeLiquid(stack, litresToDrink)?.StackSize ?? 0);

				float healthChange = nutriProps.Health * healthLossMul;

				float intox = byEntity.WatchedAttributes.GetFloat("intoxication");
				byEntity.WatchedAttributes.SetFloat("intoxication", Math.Min(1.1f, intox + nutriProps.Intoxication));

				if(healthChange != 0)
				{
					byEntity.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Internal, Type = healthChange > 0 ? EnumDamageType.Heal : EnumDamageType.Poison }, Math.Abs(healthChange));
				}

				slot.MarkDirty();
				player.InventoryManager.BroadcastHotbarSlot();
			}
		}

		public override FoodNutritionProperties GetNutritionProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
		{
			ItemStack contentStack = GetContent(itemstack);
			WaterTightContainableProps props = contentStack == null ? null : GetContainableProps(contentStack);

			if(props?.NutritionPropsPerLitre != null)
			{
				var nutriProps = props.NutritionPropsPerLitre.Clone();
				float litre = contentStack.StackSize / props.ItemsPerLitre;
				nutriProps.Health *= litre;
				nutriProps.Satiety *= litre;
				nutriProps.EatenStack = new JsonItemStack();
				nutriProps.EatenStack.ResolvedItemstack = itemstack.Clone();
				nutriProps.EatenStack.ResolvedItemstack.StackSize = 1;
				(nutriProps.EatenStack.ResolvedItemstack.Collectible as ILiquidSink).SetContent(nutriProps.EatenStack.ResolvedItemstack, null);

				return nutriProps;
			}

			return base.GetNutritionProperties(world, itemstack, forEntity);
		}

		public bool TryFillFromBlock(ItemSlot itemslot, EntityAgent byEntity, BlockPos pos)
		{
			IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
			IBlockAccessor blockAcc = byEntity.World.BlockAccessor;

			Block block = blockAcc.GetBlock(pos);
			if(block.Attributes?["waterTightContainerProps"].Exists == false) return false;

			WaterTightContainableProps props = block.Attributes?["waterTightContainerProps"]?.AsObject<WaterTightContainableProps>();
			if(props?.WhenFilled == null || !props.Containable) return false;

			props.WhenFilled.Stack.Resolve(byEntity.World, "liquidcontainerbase");

			if(GetCurrentLitres(itemslot.Itemstack) >= CapacityLitres) return false;


			var contentStack = props.WhenFilled.Stack.ResolvedItemstack.Clone();
			var cprops = GetContainableProps(contentStack);
			contentStack.StackSize = 999999;

			int moved = splitStackAndPerformAction(byEntity, itemslot, (stack) => TryPutLiquid(stack, contentStack, CapacityLitres));

			if(moved > 0)
			{
				DoLiquidMovedEffects(byPlayer, contentStack, moved, EnumLiquidDirection.Fill);
			}

			return true;
		}

		public virtual void TryFillFromBlock(EntityItem byEntityItem, BlockPos pos)
		{
			IWorldAccessor world = byEntityItem.World;

			Block block = world.BlockAccessor.GetBlock(pos);

			if(block.Attributes?["waterTightContainerProps"].Exists == false) return;
			WaterTightContainableProps props = block.Attributes?["waterTightContainerProps"].AsObject<WaterTightContainableProps>();
			if(props?.WhenFilled == null || !props.Containable) return;

			if(props.WhenFilled.Stack.ResolvedItemstack == null) props.WhenFilled.Stack.Resolve(world, "liquidcontainerbase");

			ItemStack whenFilledStack = props.WhenFilled.Stack.ResolvedItemstack;

			ItemStack contentStack = GetContent(byEntityItem.Itemstack);
			bool canFill = contentStack == null || (contentStack.Equals(world, whenFilledStack, GlobalConstants.IgnoredStackAttributes) && GetCurrentLitres(byEntityItem.Itemstack) < CapacityLitres);
			if(!canFill) return;

			contentStack.StackSize = 999999;
			int moved = splitStackAndPerformAction(byEntityItem, byEntityItem.Slot, (stack) => TryPutLiquid(stack, contentStack, CapacityLitres));
			if(moved > 0)
			{
				world.PlaySoundAt(props.FillSound, pos.X, pos.Y, pos.Z, null);
			}
		}


		private bool SpillContents(ItemSlot containerSlot, EntityAgent byEntity, BlockSelection blockSel)
		{
			BlockPos pos = blockSel.Position;
			IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
			IBlockAccessor blockAcc = byEntity.World.BlockAccessor;
			BlockPos secondPos = blockSel.Position.AddCopy(blockSel.Face);
			var contentStack = GetContent(containerSlot.Itemstack);

			WaterTightContainableProps props = GetContentProps(containerSlot.Itemstack);

			if(props == null || !props.AllowSpill || props.WhenSpilled == null) return false;

			if(!byEntity.World.Claims.TryAccess(byPlayer, secondPos, EnumBlockAccessFlags.BuildOrBreak))
			{
				return false;
			}

			var action = props.WhenSpilled.Action;
			float currentlitres = GetCurrentLitres(containerSlot.Itemstack);

			if(currentlitres > 0 && currentlitres < 10)
			{
				action = WaterTightContainableProps.EnumSpilledAction.DropContents;
			}

			if(action == WaterTightContainableProps.EnumSpilledAction.PlaceBlock)
			{
				Block waterBlock = byEntity.World.GetBlock(props.WhenSpilled.Stack.Code);

				if(props.WhenSpilled.StackByFillLevel != null)
				{
					JsonItemStack fillLevelStack;
					props.WhenSpilled.StackByFillLevel.TryGetValue((int)currentlitres, out fillLevelStack);
					if(fillLevelStack != null) waterBlock = byEntity.World.GetBlock(fillLevelStack.Code);
				}

				Block currentblock = blockAcc.GetBlock(pos);
				if(currentblock.Replaceable >= 6000)
				{
					blockAcc.SetBlock(waterBlock.BlockId, pos);
					blockAcc.TriggerNeighbourBlockUpdate(pos);
					blockAcc.MarkBlockDirty(pos);
				}
				else
				{
					if(blockAcc.GetBlock(secondPos).Replaceable >= 6000)
					{
						blockAcc.SetBlock(waterBlock.BlockId, secondPos);
						blockAcc.TriggerNeighbourBlockUpdate(pos);
						blockAcc.MarkBlockDirty(secondPos);
					}
					else
					{
						return false;
					}
				}
			}

			if(action == WaterTightContainableProps.EnumSpilledAction.DropContents)
			{
				props.WhenSpilled.Stack.Resolve(byEntity.World, "liquidcontainerbasespill");

				ItemStack stack = props.WhenSpilled.Stack.ResolvedItemstack.Clone();
				stack.StackSize = contentStack.StackSize;

				byEntity.World.SpawnItemEntity(stack, blockSel.Position.ToVec3d().Add(blockSel.HitPosition));
			}


			int moved = splitStackAndPerformAction(byEntity, containerSlot, (stack) => { SetContent(stack, null); return contentStack.StackSize; });

			DoLiquidMovedEffects(byPlayer, contentStack, moved, EnumLiquidDirection.Pour);
			return true;
		}
		#endregion

		public enum EnumLiquidDirection { Fill, Pour }
		public void DoLiquidMovedEffects(IPlayer player, ItemStack contentStack, int moved, EnumLiquidDirection dir)
		{
			if(player == null) return;

			WaterTightContainableProps props = GetContainableProps(contentStack);
			float litresMoved = moved / props.ItemsPerLitre;

			(player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			api.World.PlaySoundAt(dir == EnumLiquidDirection.Fill ? props.FillSound : props.PourSound, player.Entity, player, true, 16, GameMath.Clamp(litresMoved / 5f, 0.35f, 1f));
			api.World.SpawnCubeParticles(player.Entity.Pos.AheadCopy(0.25).XYZ.Add(0, player.Entity.SelectionBox.Y2 / 2, 0), contentStack, 0.75f, (int)litresMoved * 2, 0.45f);
		}

		private int splitStackAndPerformAction(Entity byEntity, ItemSlot slot, System.Func<ItemStack, int> action)
		{
			if(slot.Itemstack.StackSize == 1)
			{
				int moved = action(slot.Itemstack);

				if(moved > 0)
				{
					int maxstacksize = slot.Itemstack.Collectible.MaxStackSize;

					(byEntity as EntityPlayer)?.WalkInventory((pslot) => {
						if(pslot.Empty || pslot is ItemSlotCreative || pslot.StackSize == pslot.Itemstack.Collectible.MaxStackSize) return true;
						int mergableq = slot.Itemstack.Collectible.GetMergableQuantity(slot.Itemstack, pslot.Itemstack, EnumMergePriority.DirectMerge);
						if(mergableq == 0) return true;

						var selfLiqBlock = slot.Itemstack.Collectible as ILiquidInterface;
						var invLiqBlock = pslot.Itemstack.Collectible as ILiquidInterface;

						if((selfLiqBlock?.GetContent(slot.Itemstack)?.StackSize ?? 0) != (invLiqBlock?.GetContent(pslot.Itemstack)?.StackSize ?? 0)) return true;

						slot.Itemstack.StackSize += mergableq;
						pslot.TakeOut(mergableq);

						slot.MarkDirty();
						pslot.MarkDirty();
						return true;
					});
				}

				return moved;
			}
			else
			{
				ItemStack containerStack = slot.Itemstack.Clone();
				containerStack.StackSize = 1;

				int moved = action(containerStack);

				if(moved > 0)
				{
					slot.TakeOut(1);
					if((byEntity as EntityPlayer)?.Player.InventoryManager.TryGiveItemstack(containerStack, true) != true)
					{
						api.World.SpawnItemEntity(containerStack, byEntity.SidedPos.XYZ);
					}

					slot.MarkDirty();
				}

				return moved;
			}
		}

		public override void OnGroundIdle(EntityItem entityItem)
		{
			base.OnGroundIdle(entityItem);

			IWorldAccessor world = entityItem.World;
			if(world.Side != EnumAppSide.Server) return;

			if(entityItem.Swimming && world.Rand.NextDouble() < 0.03)
			{
				TryFillFromBlock(entityItem, entityItem.SidedPos.AsBlockPos);
			}

			if(entityItem.Swimming && world.Rand.NextDouble() < 0.01)
			{
				ItemStack[] stacks = GetContents(world, entityItem.Itemstack);
				if(MealMeshCache.ContentsRotten(stacks))
				{
					for(int i = 0; i < stacks.Length; i++)
					{
						if(stacks[i] != null && stacks[i].StackSize > 0 && stacks[i].Collectible.Code.Path == "rot")
						{
							world.SpawnItemEntity(stacks[i], entityItem.ServerPos.XYZ);
						}
					}

					SetContent(entityItem.Itemstack, null);
				}
			}
		}

		public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
			base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

			GetContentInfo(inSlot, dsc, world);
		}

		public virtual void GetContentInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world)
		{
			float litres = GetCurrentLitres(inSlot.Itemstack);
			ItemStack contentStack = GetContent(inSlot.Itemstack);

			if(litres <= 0) dsc.Append(Lang.Get("Empty"));

			else
			{
				string incontainerrname = Lang.Get("incontainer-" + contentStack.Class.ToString().ToLowerInvariant() + "-" + contentStack.Collectible.Code.Path);
				if(litres == 1)
				{
					dsc.Append(Lang.Get("Contents: {0} litre of {1}", litres, incontainerrname));
				}
				else
				{
					dsc.Append(Lang.Get("Contents: {0} litres of {1}", litres, incontainerrname));
				}
			}
		}

		public override void TryMergeStacks(ItemStackMergeOperation op)
		{
			op.MovableQuantity = GetMergableQuantity(op.SinkSlot.Itemstack, op.SourceSlot.Itemstack, op.CurrentPriority);
			if(op.MovableQuantity == 0) return;
			if(!op.SinkSlot.CanTakeFrom(op.SourceSlot, op.CurrentPriority)) return;

			ItemStack sinkContent = GetContent(op.SinkSlot.Itemstack);
			ItemStack sourceContent = GetContent(op.SourceSlot.Itemstack);

			if(sinkContent == null && sourceContent == null)
			{
				base.TryMergeStacks(op);
				return;
			}

			if(sinkContent == null || sourceContent == null) { op.MovableQuantity = 0; return; }

			if(!sinkContent.Equals(op.World, sourceContent, GlobalConstants.IgnoredStackAttributes)) { op.MovableQuantity = 0; return; }

			float sourceLitres = GetCurrentLitres(op.SourceSlot.Itemstack);
			float sinkLitres = GetCurrentLitres(op.SourceSlot.Itemstack);

			float sourceCapLitres = (op.SourceSlot.Itemstack.Collectible as ILiquidInterface)?.CapacityLitres ?? 0;
			float sinkCapLitres = (op.SinkSlot.Itemstack.Collectible as ILiquidInterface)?.CapacityLitres ?? 0;

			if(sourceCapLitres == 0 || sinkCapLitres == 0)
			{
				base.TryMergeStacks(op);
				return;
			}

			if(sourceLitres >= sourceCapLitres && sinkLitres >= sinkCapLitres)
			{
				// Full buckets are not stackable
				//op.MovableQuantity = 0;
				base.TryMergeStacks(op);
				return;
			}

			if(op.CurrentPriority == EnumMergePriority.DirectMerge)
			{
				float movableLitres = Math.Min(sinkCapLitres - sinkLitres, sourceLitres);
				int moved = TryPutLiquid(op.SinkSlot.Itemstack, sinkContent, movableLitres);
				DoLiquidMovedEffects(op.ActingPlayer, sinkContent, moved, EnumLiquidDirection.Pour);

				TryTakeContent(op.SourceSlot.Itemstack, moved);
				op.SourceSlot.MarkDirty();
				op.SinkSlot.MarkDirty();
			}

			op.MovableQuantity = 0;
			return;
		}

		public override bool MatchesForCrafting(ItemStack inputStack, GridRecipe gridRecipe, CraftingRecipeIngredient ingredient)
		{
			JsonObject rprops = ingredient.RecipeAttributes;
			if(rprops?.Exists != true || rprops?["requiresContent"].Exists != true) rprops = gridRecipe.Attributes?["liquidContainerProps"];

			if(rprops?.Exists != true)
			{
				return base.MatchesForCrafting(inputStack, gridRecipe, ingredient);
			}

			string contentCode = rprops["requiresContent"]["code"].AsString();
			string contentType = rprops["requiresContent"]["type"].AsString();

			ItemStack contentStack = GetContent(inputStack);

			if(contentStack == null) return false;

			float litres = rprops["requiresLitres"].AsFloat();
			var props = GetContainableProps(contentStack);
			int q = (int)(props.ItemsPerLitre * litres) / inputStack.StackSize;

			bool a = string.Equals(contentStack.Class.ToString(), contentType, StringComparison.InvariantCultureIgnoreCase);
			bool b = WildcardUtil.Match(new AssetLocation(contentCode), contentStack.Collectible.Code);
			bool c = contentStack.StackSize >= q;

			return a && b && c;
		}

		public override void OnConsumedByCrafting(ItemSlot[] allInputSlots, ItemSlot stackInSlot, GridRecipe gridRecipe, CraftingRecipeIngredient fromIngredient, IPlayer byPlayer, int quantity)
		{
			JsonObject rprops = fromIngredient.RecipeAttributes;
			if(rprops?.Exists != true || rprops?["requiresContent"].Exists != true) rprops = gridRecipe.Attributes?["liquidContainerProps"];

			if(rprops?.Exists != true)
			{
				base.OnConsumedByCrafting(allInputSlots, stackInSlot, gridRecipe, fromIngredient, byPlayer, quantity);
				return;
			}

			ItemStack contentStack = GetContent(stackInSlot.Itemstack);
			float litres = rprops["requiresLitres"].AsFloat();
			var props = GetContainableProps(contentStack);
			int q = (int)(props.ItemsPerLitre * litres / stackInSlot.StackSize);

			TryTakeContent(stackInSlot.Itemstack, q);
		}

		public static string PerishableInfoCompact(ICoreAPI Api, ItemSlot contentSlot, float ripenRate, bool withStackName = true)
		{
			StringBuilder dsc = new StringBuilder();

			if(withStackName)
			{
				dsc.Append(contentSlot.Itemstack.GetName());
			}

			TransitionState[] transitionStates = contentSlot.Itemstack?.Collectible.UpdateAndGetTransitionStates(Api.World, contentSlot);

			if(transitionStates != null)
			{
				for(int i = 0; i < transitionStates.Length; i++)
				{
					string comma = ", ";

					TransitionState state = transitionStates[i];

					TransitionableProperties prop = state.Props;
					float perishRate = contentSlot.Itemstack.Collectible.GetTransitionRateMul(Api.World, contentSlot, prop.Type);

					if(perishRate <= 0) continue;

					float transitionLevel = state.TransitionLevel;
					float freshHoursLeft = state.FreshHoursLeft / perishRate;

					switch(prop.Type)
					{
						case EnumTransitionType.Perish:


							if(transitionLevel > 0)
							{
								dsc.Append(comma + Lang.Get("{0}% spoiled", (int)Math.Round(transitionLevel * 100)));
							}
							else
							{
								double hoursPerday = Api.World.Calendar.HoursPerDay;

								if(freshHoursLeft / hoursPerday >= Api.World.Calendar.DaysPerYear)
								{
									dsc.Append(comma + Lang.Get("fresh for {0} years", Math.Round(freshHoursLeft / hoursPerday / Api.World.Calendar.DaysPerYear, 1)));
								}
								else if(freshHoursLeft > hoursPerday)
								{
									dsc.Append(comma + Lang.Get("fresh for {0} days", Math.Round(freshHoursLeft / hoursPerday, 1)));
								}
								else
								{
									dsc.Append(comma + Lang.Get("fresh for {0} hours", Math.Round(freshHoursLeft, 1)));
								}
							}
							break;

						case EnumTransitionType.Ripen:

							if(transitionLevel > 0)
							{
								dsc.Append(comma + Lang.Get("{1:0.#} days left to ripen ({0}%)", (int)Math.Round(transitionLevel * 100), (state.TransitionHours - state.TransitionedHours) / Api.World.Calendar.HoursPerDay / ripenRate));
							}
							else
							{
								double hoursPerday = Api.World.Calendar.HoursPerDay;

								if(freshHoursLeft / hoursPerday >= Api.World.Calendar.DaysPerYear)
								{
									dsc.Append(comma + Lang.Get("will ripen in {0} years", Math.Round(freshHoursLeft / hoursPerday / Api.World.Calendar.DaysPerYear, 1)));
								}
								else if(freshHoursLeft > hoursPerday)
								{
									dsc.Append(comma + Lang.Get("will ripen in {0} days", Math.Round(freshHoursLeft / hoursPerday, 1)));
								}
								else
								{
									dsc.Append(comma + Lang.Get("will ripen in {0} hours", Math.Round(freshHoursLeft, 1)));
								}
							}
							break;
					}
				}

			}

			return dsc.ToString();
		}

		ItemStack ILiquidSource.TryTakeContent(BlockPos pos, int quantity)
		{
			throw new NotImplementedException();
		}

		float ILiquidInterface.GetCurrentLitres(BlockPos pos)
		{
			throw new NotImplementedException();
		}

		bool ILiquidInterface.IsFull(BlockPos pos)
		{
			throw new NotImplementedException();
		}

		WaterTightContainableProps ILiquidInterface.GetContentProps(BlockPos pos)
		{
			throw new NotImplementedException();
		}

		ItemStack ILiquidInterface.GetContent(BlockPos pos)
		{
			throw new NotImplementedException();
		}

		void ILiquidSink.SetContent(BlockPos pos, ItemStack content)
		{
			throw new NotImplementedException();
		}

		int ILiquidSink.TryPutLiquid(BlockPos pos, ItemStack liquidStack, float desiredLitres)
		{
			throw new NotImplementedException();
		}
	}
}
