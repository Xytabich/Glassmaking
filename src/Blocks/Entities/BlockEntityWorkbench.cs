using GlassMaking.Blocks.Renderer;
using GlassMaking.Items;
using GlassMaking.Workbench;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GlassMaking.Blocks
{
	public class BlockEntityWorkbench : BlockEntityDisplay, IWorkbenchRenderInfo
	{
		public override InventoryBase Inventory => inventory;
		public override string InventoryClassName => "glassmaking:workbench";
		public override string AttributeTransformCode => "workbenchToolTransform";

		public IWorkpieceRenderer workpieceRenderer => renderer;

		protected virtual int itemCapacity => 9;

		private GlassMakingMod mod;

		private string recipeCode = null;
		private WorkbenchRecipe recipe = null;
		private int recipeStep = 0;
		private int startedStep = -1;

		private SelectionInfo[] toolsSelection;
		private WorkbenchToolsInventory inventory;

		private ItemStack workpiece = null;
		private WorkbenchWorkpieceRenderer renderer = null;

		private Cuboidf[] selectionBoxes;
		private Dictionary<string, int> toolSlots = new Dictionary<string, int>();

		private Action waitForComplete = null;
		private GuiDialog dlg = null;

		private WorkbenchRecipeStep cachedIdleToolsStep = null;

		public BlockEntityWorkbench()
		{
			inventory = new WorkbenchToolsInventory(itemCapacity, InventoryClassName + "-" + Pos, null, this);
			meshes = new MeshData[itemCapacity];
			toolsSelection = new SelectionInfo[itemCapacity];
		}

		public override void Initialize(ICoreAPI api)
		{
			mod = api.ModLoader.GetModSystem<GlassMakingMod>();
			if(!string.IsNullOrEmpty(recipeCode))
			{
				recipe = mod.GetWorkbenchRecipe(recipeCode);
			}
			base.Initialize(api);
			workpiece?.ResolveBlockOrItem(api.World);
			for(int i = itemCapacity - 1; i >= 0; i--)
			{
				var tool = inventory.GetBehavior(i);
				if(tool != null)
				{
					toolSlots[tool.ToolCode] = i;
					UpdateToolBounds(i);
				}
			}
			RebuildSelectionBoxes();
			if(api.Side == EnumAppSide.Client)
			{
				renderer = new WorkbenchWorkpieceRenderer((ICoreClientAPI)api, Pos, Block.Shape.rotateY);
				UpdateWorkpieceRenderer();
				UpdateIdleState();
			}
		}

		public WorldInteraction[] GetBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
		{
			List<WorldInteraction> list = new List<WorldInteraction>();
			if(selection.SelectionBoxIndex < 0)
			{
				if(workpiece != null)
				{
					list.Add(new WorldInteraction() {
						ActionLangCode = "glassmaking:blockhelp-workbench-takeitem",
						HotKeyCode = "sneak",
						RequireFreeHand = true,
						MouseButton = EnumMouseButton.Right,
						Itemstacks = new ItemStack[] { workpiece.Clone() }
					});
					if(recipe != null)
					{
						list.Add(new WorldInteraction() {
							ActionLangCode = "glassmaking:blockhelp-workbench-craft",
							MouseButton = EnumMouseButton.Right
						});
					}
				}
				else
				{
					list.Add(new WorldInteraction() {
						ActionLangCode = "glassmaking:blockhelp-workbench-recipe",
						HotKeyCode = "sneak",
						MouseButton = EnumMouseButton.Right
					});
				}
				list.Add(new WorldInteraction() {
					ActionLangCode = "glassmaking:blockhelp-workbench-addtool",
					HotKeyCode = "sprint",
					MouseButton = EnumMouseButton.Right
				});
			}
			else
			{
				for(int i = 0; i < toolsSelection.Length; i++)
				{
					if(toolsSelection[i] != null && toolsSelection[i].index >= selection.SelectionBoxIndex &&
						selection.SelectionBoxIndex < (toolsSelection[i].index + toolsSelection[i].boxes.Length))
					{
						selection = selection.Clone();
						selection.SelectionBoxIndex -= toolsSelection[i].index;
						var arr = inventory.GetBehavior(i).GetBlockInteractionHelp(world, selection, forPlayer, recipe, recipeStep);
						list.Add(new WorldInteraction() {
							ActionLangCode = "glassmaking:blockhelp-workbench-taketool",
							HotKeyCode = "sprint",
							RequireFreeHand = true,
							MouseButton = EnumMouseButton.Right,
							Itemstacks = new ItemStack[] { inventory[i].Itemstack.Clone() }
						});
						if(arr != null && arr.Length > 0)
						{
							list.AddRange(arr);
						}
						break;
					}
				}
			}
			return list.ToArray();
		}

		public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
		{
			foreach(BlockEntityBehavior behavior in Behaviors)
			{
				behavior.GetBlockInfo(forPlayer, dsc);
			}
		}

		public bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection selection, ref EnumHandling handling)
		{
			handling = EnumHandling.PassThrough;
			ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
			ItemStack itemstack = slot.Itemstack;

			if(byPlayer.Entity.Controls.Sprint)
			{
				if(selection.SelectionBoxIndex < 0)
				{
					if(itemstack != null && TryAddTool(byPlayer, slot))
					{
						if(world.Side == EnumAppSide.Client)
						{
							((IClientPlayer)byPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
						}
						handling = EnumHandling.PreventSubsequent;
						return true;
					}
				}
				else
				{
					if(itemstack == null)
					{
						for(int i = 0; i < toolsSelection.Length; i++)
						{
							if(toolsSelection[i] != null && toolsSelection[i].index >= selection.SelectionBoxIndex &&
								selection.SelectionBoxIndex < (toolsSelection[i].index + toolsSelection[i].boxes.Length))
							{
								var behavior = inventory.GetBehavior(i);
								if(recipe != null && startedStep >= 0 && startedStep == recipeStep && recipe.Steps[recipeStep].Tools.ContainsKey(behavior.ToolCode))
								{
									CancelCurrentStep(0, world, byPlayer, selection);
								}

								slot.Itemstack = inventory[i].Itemstack.Clone();

								inventory.SetItem(i, null);

								toolsSelection[i] = null;
								if(behavior != null) toolSlots.Remove(behavior.ToolCode);

								RebuildSelectionBoxes();
								updateMesh(i);

								MarkDirty(true);
								slot.MarkDirty();
								handling = EnumHandling.PreventSubsequent;
								return true;
							}
						}
					}
				}
				return false;
			}

			if(workpiece == null)
			{
				if(selection.SelectionBoxIndex < 0)
				{
					if(itemstack != null)
					{
						if(itemstack.Collectible is ItemGlassWorkpiece)
						{
							if(Api.Side == EnumAppSide.Server)
							{
								var recipeInfo = itemstack.Attributes.GetTreeAttribute("glassmaking:recipe");
								if(recipeInfo != null)
								{
									var recipe = mod.GetWorkbenchRecipe(recipeInfo.GetString("code"));
									if(recipe != null)
									{
										this.recipe = recipe;
										this.recipeStep = recipeInfo.GetInt("step", 0);
										this.workpiece = slot.TakeOut(1);

										MarkDirty(true);
										slot.MarkDirty();
										handling = EnumHandling.PreventSubsequent;
										return true;
									}
								}
							}
						}
						else if(Api.Side == EnumAppSide.Client)
						{
							OpenDialog(itemstack);
							handling = EnumHandling.PreventSubsequent;
							return false;
						}
					}
				}
				return false;
			}
			else
			{
				if(byPlayer.Entity.Controls.Sneak)
				{
					if(selection.SelectionBoxIndex < 0)
					{
						if(itemstack == null && workpiece != null)
						{
							CancelStartedStep(0, world, byPlayer, selection);

							slot.Itemstack = workpiece.Clone();
							workpiece = null;
							recipe = null;
							recipeStep = -1;

							MarkDirty(true);
							slot.MarkDirty();
							handling = EnumHandling.PreventSubsequent;
							UpdateWorkpieceRenderer();
							return true;
						}
					}
					return false;
				}

				if(recipe != null)
				{
					startedStep = recipeStep;
					UpdateIdleState();

					var sel = selection.Clone();
					bool isStarted = true;
					foreach(var pair in recipe.Steps[recipeStep].Tools)
					{
						if(toolSlots.TryGetValue(pair.Key, out var slotId))
						{
							sel.CopyFrom(selection);
							sel.SelectionBoxIndex -= toolsSelection[slotId].index;
							isStarted &= inventory.GetBehavior(slotId).OnUseStart(world, byPlayer, sel, recipe, recipeStep);
						}
						else
						{
							isStarted = false;
							break;
						}
					}

					handling = EnumHandling.PreventSubsequent;
					if(isStarted)
					{
						return !TryCompleteStep(0, world, byPlayer, selection);
					}

					CancelCurrentStep(0, world, byPlayer, selection);
					return false;
				}
			}

			return false;
		}

		public bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection selection, ref EnumHandling handling)
		{
			handling = EnumHandling.PassThrough;
			if(selection.SelectionBoxIndex >= 0 || byPlayer.Entity.Controls.Sneak || byPlayer.Entity.Controls.Sprint || recipe == null)
			{
				CancelStartedStep(secondsUsed, world, byPlayer, selection);
				return false;
			}

			var sel = selection.Clone();
			bool canContinue = true;
			foreach(var pair in recipe.Steps[recipeStep].Tools)
			{
				if(toolSlots.TryGetValue(pair.Key, out var slotId))
				{
					sel.CopyFrom(selection);
					sel.SelectionBoxIndex -= toolsSelection[slotId].index;
					canContinue &= inventory.GetBehavior(slotId).OnUseStep(secondsUsed, world, byPlayer, sel, recipe, recipeStep);
				}
				else
				{
					canContinue = false;
					break;
				}
			}
			handling = EnumHandling.PreventSubsequent;
			if(canContinue)
			{
				return !TryCompleteStep(secondsUsed, world, byPlayer, selection);
			}

			CancelCurrentStep(secondsUsed, world, byPlayer, selection);
			return false;
		}

		public void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection selection, ref EnumHandling handling)
		{
			if(CancelStartedStep(secondsUsed, world, byPlayer, selection))
			{
				handling = EnumHandling.PreventSubsequent;
			}
		}

		public bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection selection, ref EnumHandling handling)
		{
			if(CancelStartedStep(secondsUsed, world, byPlayer, selection))
			{
				handling = EnumHandling.PreventSubsequent;
				return true;
			}
			return false;
		}

		public Cuboidf[] GetToolSelectionBoxes()
		{
			return selectionBoxes;
		}

		public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
		{
			recipeCode = tree.GetString("recipe");
			int newRecipeStep = this.recipeStep;
			WorkbenchRecipe newRecipe = this.recipe;
			if(string.IsNullOrEmpty(recipeCode))
			{
				newRecipe = null;
				newRecipeStep = -1;
			}
			else
			{
				if(Api?.World != null)
				{
					newRecipe = mod.GetWorkbenchRecipe(recipeCode);
				}
				newRecipeStep = tree.GetInt("step");
			}
			if(waitForComplete != null)
			{
				if(this.recipeStep != newRecipeStep || this.recipe != newRecipe)
				{
					var waitForComplete = this.waitForComplete;
					this.waitForComplete = null;
					waitForComplete.Invoke();
				}
			}
			this.recipe = newRecipe;
			this.recipeStep = newRecipeStep;

			base.FromTreeAttributes(tree, worldForResolving);
			workpiece = tree.GetItemstack("workpiece");
			if(Api?.World != null)
			{
				workpiece?.ResolveBlockOrItem(Api.World);
				if(inventory.modifiedSlots.Count > 0)
				{
					toolSlots.Clear();
					for(int i = itemCapacity - 1; i >= 0; i--)
					{
						var tool = inventory.GetBehavior(i);
						if(tool != null) toolSlots[tool.ToolCode] = i;
					}
					for(int i = inventory.modifiedSlots.Count - 1; i >= 0; i--)
					{
						UpdateToolBounds(inventory.modifiedSlots[i]);
					}
					RebuildSelectionBoxes();
				}
				UpdateWorkpieceRenderer();
				UpdateIdleState();
			}
		}

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
			base.ToTreeAttributes(tree);
			tree.SetItemstack("workpiece", workpiece);
			if(recipe != null)
			{
				tree.SetString("recipe", recipe.Code.ToShortString());
				tree.SetInt("step", recipeStep);
			}
		}

		public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed)
		{
			base.OnLoadCollectibleMappings(worldForResolve, oldBlockIdMapping, oldItemIdMapping, schematicSeed);
			if(workpiece != null)
			{
				workpiece.Collectible.OnLoadCollectibleMappings(worldForResolve, new DummySlot(workpiece), oldBlockIdMapping, oldItemIdMapping);
			}
		}

		public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
		{
			base.OnStoreCollectibleMappings(blockIdMapping, itemIdMapping);
			if(workpiece != null)
			{
				workpiece.Collectible.OnStoreCollectibleMappings(Api.World, new DummySlot(workpiece), blockIdMapping, itemIdMapping);
			}
		}

		public override void OnBlockRemoved()
		{
			dlg?.TryClose();
			dlg?.Dispose();
			renderer?.Dispose();
			base.OnBlockRemoved();
			for(int i = itemCapacity - 1; i >= 0; i--)
			{
				var tool = inventory.GetBehavior(i);
				if(tool != null) tool.OnBlockRemoved();
			}
		}

		public override void OnBlockUnloaded()
		{
			dlg?.TryClose();
			dlg?.Dispose();
			renderer?.Dispose();
			base.OnBlockUnloaded();
			for(int i = itemCapacity - 1; i >= 0; i--)
			{
				var tool = inventory.GetBehavior(i);
				if(tool != null) tool.OnBlockUnloaded();
			}
		}

		public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
		{
			base.OnReceivedClientPacket(fromPlayer, packetid, data);
			if(packetid == 1001)
			{
				string code = SerializerUtil.Deserialize<string>(data);
				var recipe = mod.GetWorkbenchRecipe(code);
				if(recipe == null)
				{
					Api.World.Logger.Error("Client tried to selected workbench recipe with code {0}, but no such recipe exists!", code);
					return;
				}
				var itemstack = fromPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;
				if(itemstack != null && recipe.Input.SatisfiesAsIngredient(itemstack))
				{
					this.recipe = recipe;
					this.recipeStep = 0;
					this.workpiece = fromPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(recipe.Input.Quantity);
					MarkDirty(true);
				}
			}
		}

		protected override MeshData genMesh(ItemStack stack)
		{
			MeshData mesh = GenItemMesh(stack);
			if(mesh == null) return null;

			if(stack.Collectible.Attributes?[AttributeTransformCode].Exists == true)
			{
				ModelTransform transform = stack.Collectible.Attributes[AttributeTransformCode].AsObject<ModelTransform>();
				transform.EnsureDefaultValues();
				mesh.ModelTransform(transform);

				mesh.Rotate(new Vec3f(0.5f, 0f, 0.5f), 0f, Block.Shape.rotateY * GameMath.DEG2RAD, 0f);
			}

			if(stack.Class == EnumItemClass.Item && (stack.Item.Shape == null || stack.Item.Shape.VoxelizeTexture))
			{
				mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), GameMath.PIHALF, 0, 0);
				mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.33f, 0.5f, 0.33f);
				mesh.Translate(0, -7.5f / 16f, 0f);
			}

			return mesh;
		}

		private MeshData GenItemMesh(ItemStack stack)
		{
			MeshData mesh;
			var dynBlock = stack.Collectible as IContainedMeshSource;

			if(dynBlock != null)
			{
				mesh = dynBlock.GenMesh(stack, capi.BlockTextureAtlas, Pos);
				if(mesh != null) mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, Block.Shape.rotateY * GameMath.DEG2RAD, 0);
			}
			else
			{
				ICoreClientAPI capi = Api as ICoreClientAPI;
				if(stack.Class == EnumItemClass.Block)
				{
					mesh = capi.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
				}
				else
				{
					nowTesselatingObj = stack.Collectible;
					nowTesselatingShape = null;
					if(stack.Item.Shape != null)
					{
						nowTesselatingShape = capi.TesselatorManager.GetCachedShape(stack.Item.Shape.Base);
					}
					capi.Tesselator.TesselateItem(stack.Item, out mesh, this);

					mesh.RenderPassesAndExtraBits.Fill((short)EnumChunkRenderPass.BlendNoCull);
				}
			}
			return mesh;
		}

		private bool TryAddTool(IPlayer byPlayer, ItemSlot slot)
		{
			if(!(slot.Itemstack.Collectible is IWorkbenchTool tool)) return false;
			var world = byPlayer.Entity.World;
			var boxes = GetRotatedBoxes(tool.GetToolBoundingBoxes(world, slot.Itemstack), Block.Shape.rotateY);
			for(int i = itemCapacity - 1; i >= 0; i--)
			{
				if(toolsSelection[i] != null)
				{
					if(HasIntersections(boxes, toolsSelection[i].boxes))
					{
						return false;
					}
				}
			}

			var toolCode = tool.GetToolCode(world, slot.Itemstack);
			if(toolSlots.ContainsKey(toolCode))
			{
				return false;
			}

			for(int i = 0; i < itemCapacity; i++)
			{
				if(inventory[i].Empty)
				{
					inventory.SetItem(i, slot.TakeOut(1));

					var behavior = inventory.GetBehavior(i);
					toolsSelection[i] = new SelectionInfo() { boxes = boxes };
					toolSlots.Add(behavior.ToolCode, i);

					RebuildSelectionBoxes();
					updateMesh(i);

					slot.MarkDirty();
					MarkDirty(true);
					return true;
				}
			}

			return false;
		}

		private void UpdateWorkpieceRenderer()
		{
			if(renderer != null)
			{
				renderer.SetItemRenderInfo(workpiece == null ? null : capi.Render.GetItemStackRenderInfo(new DummySlot(workpiece), EnumItemRenderTarget.Ground));
			}
		}

		private void UpdateIdleState()
		{
			if(Api.Side == EnumAppSide.Client)
			{
				UpdateWorkpieceMatrix();

				var world = Api.World;
				if(recipe == null || startedStep >= 0 && recipeStep == startedStep)
				{
					if(cachedIdleToolsStep != null)
					{
						foreach(var pair in cachedIdleToolsStep.Tools)
						{
							if(toolSlots.TryGetValue(pair.Key, out var slotId))
							{
								inventory.GetBehavior(slotId).OnIdleStop(world, recipe, recipeStep);
							}
						}
						cachedIdleToolsStep = null;
					}
				}
				else
				{
					cachedIdleToolsStep = recipe.Steps[recipeStep];
					foreach(var pair in cachedIdleToolsStep.Tools)
					{
						if(toolSlots.TryGetValue(pair.Key, out var slotId))
						{
							inventory.GetBehavior(slotId).OnIdleStart(world, recipe, recipeStep);
						}
					}
				}
			}
		}

		private void UpdateWorkpieceMatrix()
		{
			if(renderer != null && workpiece != null)
			{
				if(workpiece.ItemAttributes?["workbenchItemTransform"].Exists == true)
				{
					var mat = workpiece.ItemAttributes["workbenchItemTransform"].AsObject<ModelTransform>().EnsureDefaultValues();
					mat.CopyTo(workpieceRenderer.itemTransform);
				}
				else
				{
					((BlockWorkbench)Block).defaultWorkpieceTransform.CopyTo(workpieceRenderer.itemTransform);
				}
			}
		}

		private void UpdateToolBounds(int slotId)
		{
			var tool = inventory.GetBehavior(slotId);
			if(tool == null) toolsSelection[slotId] = null;
			else
			{
				toolsSelection[slotId] = new SelectionInfo() {
					boxes = GetRotatedBoxes(tool.GetBoundingBoxes(), Block.Shape.rotateY)
				};
			}
		}

		private bool CancelStartedStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection selection)
		{
			if(recipe != null && startedStep >= 0 && recipeStep == startedStep)
			{
				CancelCurrentStep(secondsUsed, world, byPlayer, selection);
				return true;
			}
			return false;
		}

		private void CancelCurrentStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection selection)
		{
			startedStep = -1;
			waitForComplete = null;
			var sel = selection.Clone();
			foreach(var pair in recipe.Steps[recipeStep].Tools)
			{
				if(toolSlots.TryGetValue(pair.Key, out var slotId))
				{
					sel.CopyFrom(selection);
					sel.SelectionBoxIndex -= toolsSelection[slotId].index;
					inventory.GetBehavior(slotId).OnUseCancel(secondsUsed, world, byPlayer, sel, recipe, recipeStep);
				}
			}
			MarkDirty(true);
			UpdateIdleState();
		}

		private bool TryCompleteStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection selection)
		{
			if(Api.Side == EnumAppSide.Client)
			{
				waitForComplete = () => CallOnUseComplete(secondsUsed, world, byPlayer, selection);
				return false;
			}

			var time = recipe.Steps[recipeStep].UseTime;
			if(!time.HasValue || secondsUsed >= time.Value)
			{
				CallOnUseComplete(secondsUsed, world, byPlayer, selection);

				recipeStep++;
				if(recipeStep >= recipe.Steps.Length)
				{
					workpiece = recipe.Output.ResolvedItemstack.Clone();
					recipe = null;
					startedStep = -1;
					recipeStep = -1;
				}
				else
				{
					if(!(workpiece.Collectible is ItemGlassWorkpiece))
					{
						workpiece = new ItemStack(world.GetItem(new AssetLocation("glassmaking:workpiece")));
					}
					var recipeInfo = workpiece.Attributes.GetOrAddTreeAttribute("glassmaking:recipe");
					recipeInfo.SetString("code", recipe.Code.ToShortString());
					recipeInfo.SetInt("step", recipeStep);
				}
				MarkDirty(true);
				return true;
			}
			return false;
		}

		private void CallOnUseComplete(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection selection)
		{
			var sel = selection.Clone();
			foreach(var pair in recipe.Steps[recipeStep].Tools)
			{
				if(toolSlots.TryGetValue(pair.Key, out var slotId))
				{
					sel.CopyFrom(selection);
					sel.SelectionBoxIndex -= toolsSelection[slotId].index;
					inventory.GetBehavior(slotId).OnUseComplete(secondsUsed, world, byPlayer, sel, recipe, recipeStep);
				}
			}
		}

		private void OpenDialog(ItemStack ingredient)
		{
			if(mod.TryFindWorkbenchRecipes(ingredient, out var recipes))
			{
				var outputs = Array.ConvertAll(recipes, r => r.Output.ResolvedItemstack);
				ICoreClientAPI capi = Api as ICoreClientAPI;
				dlg?.Dispose();
				dlg = new GuiDialogBlockEntityRecipeSelector(Lang.Get("glassmaking:Select workbench recipe"), outputs, selectedIndex => {
					capi.Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, 1001, SerializerUtil.Serialize(recipes[selectedIndex].Code.ToShortString()));
				}, () => { }, Pos, capi);
				dlg.TryOpen();
			}
		}

		private void RebuildSelectionBoxes()
		{
			var boxes = new List<Cuboidf>();
			for(int i = itemCapacity - 1; i >= 0; i--)
			{
				var tool = inventory.GetBehavior(i);
				if(tool != null)
				{
					toolsSelection[i].index = boxes.Count;
					boxes.AddRange(toolsSelection[i].boxes);
				}
			}
			selectionBoxes = boxes.ToArray();
		}

		private static Cuboidf[] GetRotatedBoxes(Cuboidf[] source, float rotation)
		{
			if(rotation == 0f) return source;
			var boxes = new Cuboidf[source.Length];
			float[] verts = new float[6];
			float[] mat = Mat4f.Create();
			Mat4f.RotateY(mat, mat, rotation * GameMath.DEG2RAD);
			var origin = new Vec3f(0.5f, 0f, 0.5f);
			for(int i = source.Length - 1; i >= 0; i--)
			{
				FillVerts(verts, source[i]);
				Mat4f.MulWithVec3_Position_WithOrigin(mat, verts, verts, 0, origin);
				Mat4f.MulWithVec3_Position_WithOrigin(mat, verts, verts, 3, origin);
				FillCuboid(verts, boxes[i] = new Cuboidf());
			}
			return boxes;
		}

		private static void FillVerts(float[] verts, Cuboidf source)
		{
			verts[0] = source.X1;
			verts[1] = source.Y1;
			verts[2] = source.Z1;
			verts[3] = source.X2;
			verts[4] = source.Y2;
			verts[5] = source.Z2;
		}

		private static void FillCuboid(float[] verts, Cuboidf target)
		{
			target.X1 = Math.Min(verts[0], verts[3]);
			target.Y1 = Math.Min(verts[1], verts[4]);
			target.Z1 = Math.Min(verts[2], verts[5]);
			target.X2 = Math.Max(verts[0], verts[3]);
			target.Y2 = Math.Max(verts[1], verts[4]);
			target.Z2 = Math.Max(verts[2], verts[5]);
		}

		private static bool HasIntersections(Cuboidf[] a, Cuboidf[] b)
		{
			for(int i = a.Length - 1; i >= 0; i--)
			{
				for(int j = b.Length - 1; j >= 0; j--)
				{
					if(a[i].Intersects(b[j])) return true;
				}
			}
			return false;
		}
		private class SelectionInfo
		{
			public int index;
			public Cuboidf[] boxes;
		}
	}
}