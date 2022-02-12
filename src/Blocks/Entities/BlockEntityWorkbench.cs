using GlassMaking.Workbench;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GlassMaking.Blocks
{
	public class BlockEntityWorkbench : BlockEntityDisplay
	{
		public override InventoryBase Inventory => inventory;
		public override string InventoryClassName => "glassmaking:workbench";
		public override string AttributeTransformCode => "workbenchToolTransform";

		protected virtual int itemCapacity => 9;

		private GlassMakingMod mod;

		private WorkbenchRecipe recipe = null;
		private int recipeStep = 0;
		private int startedStep = -1;

		private int blockBoxesCount = 0;
		private SelectionInfo[] toolsSelection;
		private WorkbenchToolsInventory inventory;

		private ItemStack ingredient = null;

		private Cuboidf[] selectionBoxes;
		private Dictionary<string, WorkbenchToolBehavior> tools = new Dictionary<string, WorkbenchToolBehavior>();

		public BlockEntityWorkbench()
		{
			inventory = new WorkbenchToolsInventory(itemCapacity, InventoryClassName + "-" + Pos, null, this);
			meshes = new MeshData[itemCapacity];
			toolsSelection = new SelectionInfo[itemCapacity];
		}

		public override void Initialize(ICoreAPI api)
		{
			mod = api.ModLoader.GetModSystem<GlassMakingMod>();
			base.Initialize(api);
			ingredient?.ResolveBlockOrItem(api.World);
			for(int i = itemCapacity - 1; i >= 0; i--)
			{
				var tool = inventory.GetBehavior(i);
				if(tool != null)
				{
					tools[tool.ToolCode] = tool;
					UpdateToolBounds(i);
				}
			}
			RebuildSelectionBoxes();
		}

		public WorldInteraction[] GetBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
		{
			if(selection.SelectionBoxIndex < blockBoxesCount)
			{
				//TODO: if recipe - show recipe help
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
						return inventory.GetBehavior(i).GetBlockInteractionHelp(world, selection, forPlayer);
					}
				}
			}
			return null;
		}

		public bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection selection, ref EnumHandling handling)
		{
			handling = EnumHandling.PassThrough;
			ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
			if(selection.SelectionBoxIndex < blockBoxesCount)
			{
				if(byPlayer.Entity.Controls.Sneak)
				{
					if(slot.Itemstack == null && ingredient != null)
					{
						slot.Itemstack = ingredient.Clone();
						ingredient = null;
						recipe = null;
						MarkDirty(true);
						handling = EnumHandling.PreventSubsequent;
						return false;
					}
				}
				else if(recipe != null)
				{
					startedStep = recipeStep;
					if(tools.TryGetValue(recipe.Steps[recipeStep].Tool, out var tool))
					{
						selection = selection.Clone();
						selection.SelectionBoxIndex = -1;
						var behResult = tool.OnBlockInteractStart(world, byPlayer, selection, ref handling);
						if(handling != EnumHandling.PassThrough) return behResult;
					}
					handling = EnumHandling.PreventSubsequent;
					return false;
				}
			}
			else
			{
				for(int i = 0; i < toolsSelection.Length; i++)
				{
					if(toolsSelection[i] != null && toolsSelection[i].index >= selection.SelectionBoxIndex &&
						selection.SelectionBoxIndex < (toolsSelection[i].index + toolsSelection[i].boxes.Length))
					{
						if(byPlayer.Entity.Controls.Sneak)
						{
							//TODO: remove tool & recalc
							handling = EnumHandling.PreventSubsequent;
							return false;
						}
						else
						{
							selection = selection.Clone();
							selection.SelectionBoxIndex -= toolsSelection[i].index;
							var behResult = inventory.GetBehavior(i).OnBlockInteractStart(world, byPlayer, selection, ref handling);
							if(handling != EnumHandling.PassThrough) return behResult;
						}
						break;
					}
				}
			}

			ItemStack itemstack = slot.Itemstack;
			if(itemstack != null)
			{
				if(byPlayer.Entity.Controls.Sneak && OnUseItem(byPlayer, slot))
				{
					if(world.Side == EnumAppSide.Client)
					{
						((IClientPlayer)byPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
					}
					handling = EnumHandling.PreventSubsequent;
					return true;
				}
			}
			return false;
		}

		public bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection selection, ref EnumHandling handling)
		{
			handling = EnumHandling.PassThrough;
			if(selection.SelectionBoxIndex < blockBoxesCount)
			{
				if(recipe != null && startedStep == recipeStep)
				{
					if(tools.TryGetValue(recipe.Steps[recipeStep].Tool, out var tool))
					{
						selection = selection.Clone();
						selection.SelectionBoxIndex = -1;
						return tool.OnBlockInteractStep(secondsUsed, world, byPlayer, selection, ref handling);
					}
					else
					{
						handling = EnumHandling.PreventSubsequent;
						return false;
					}
				}
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
						return inventory.GetBehavior(i).OnBlockInteractStep(secondsUsed, world, byPlayer, selection, ref handling);
					}
				}
			}
			return false;
		}

		public void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection selection, ref EnumHandling handling)
		{
			handling = EnumHandling.PassThrough;
			if(selection.SelectionBoxIndex < blockBoxesCount)
			{
				if(recipe != null && startedStep == recipeStep)
				{
					if(tools.TryGetValue(recipe.Steps[recipeStep].Tool, out var tool))
					{
						selection = selection.Clone();
						selection.SelectionBoxIndex = -1;
						tool.OnBlockInteractStop(secondsUsed, world, byPlayer, selection, ref handling);
					}
					handling = EnumHandling.PreventSubsequent;
				}
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
						inventory.GetBehavior(i).OnBlockInteractStop(secondsUsed, world, byPlayer, selection, ref handling);
					}
				}
			}
		}

		public bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection selection, ref EnumHandling handling)
		{
			handling = EnumHandling.PassThrough;
			if(selection.SelectionBoxIndex < blockBoxesCount)
			{
				if(recipe != null && startedStep == recipeStep)
				{
					if(tools.TryGetValue(recipe.Steps[recipeStep].Tool, out var tool))
					{
						selection = selection.Clone();
						selection.SelectionBoxIndex = -1;
						return tool.OnBlockInteractCancel(secondsUsed, world, byPlayer, selection, ref handling);
					}
					else
					{
						handling = EnumHandling.PreventSubsequent;
						return true;
					}
				}
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
						return inventory.GetBehavior(i).OnBlockInteractCancel(secondsUsed, world, byPlayer, selection, ref handling);
					}
				}
			}
			return false;
		}

		private bool OnUseItem(IPlayer byPlayer, ItemSlot slot)
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
			if(tools.ContainsKey(toolCode))
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
					tools.Add(behavior.ToolCode, behavior);

					RebuildSelectionBoxes();
					updateMesh(i);

					slot.MarkDirty();
					MarkDirty(true);
					return true;
				}
			}

			return false;
		}

		public Cuboidf[] GetSelectionBoxes()
		{
			return selectionBoxes;
		}

		public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
		{
			var code = tree.GetString("recipe");
			if(!string.IsNullOrEmpty(code))
			{
				recipe = mod.GetWorkbenchRecipe(code);
				recipeStep = tree.GetInt("step");
			}
			base.FromTreeAttributes(tree, worldForResolving);
			ingredient = tree.GetItemstack("ingredient");
			if(Api?.World != null)
			{
				ingredient?.ResolveBlockOrItem(Api.World);
				if(inventory.modifiedSlots.Count > 0)
				{
					tools.Clear();
					for(int i = itemCapacity - 1; i >= 0; i--)
					{
						var tool = inventory.GetBehavior(i);
						if(tool != null) tools[tool.ToolCode] = tool;
					}
					for(int i = inventory.modifiedSlots.Count - 1; i >= 0; i--)
					{
						UpdateToolBounds(inventory.modifiedSlots[i]);
					}
					RebuildSelectionBoxes();
				}
			}
		}

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
			base.ToTreeAttributes(tree);
			tree.SetItemstack("ingredient", ingredient);
			if(recipe != null)
			{
				tree.SetString("recipe", recipe.Code.ToShortString());
				tree.SetInt("step", recipeStep);
			}
		}

		public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed)
		{
			base.OnLoadCollectibleMappings(worldForResolve, oldBlockIdMapping, oldItemIdMapping, schematicSeed);
			if(ingredient != null)
			{
				ingredient.Collectible.OnLoadCollectibleMappings(worldForResolve, new DummySlot(ingredient), oldBlockIdMapping, oldItemIdMapping);
			}
		}

		public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
		{
			base.OnStoreCollectibleMappings(blockIdMapping, itemIdMapping);
			if(ingredient != null)
			{
				ingredient.Collectible.OnStoreCollectibleMappings(Api.World, new DummySlot(ingredient), blockIdMapping, itemIdMapping);
			}
		}

		public override void OnBlockRemoved()
		{
			base.OnBlockRemoved();
			for(int i = itemCapacity - 1; i >= 0; i--)
			{
				var tool = inventory.GetBehavior(i);
				if(tool != null) tool.OnBlockRemoved();
			}
		}

		public override void OnBlockUnloaded()
		{
			base.OnBlockUnloaded();
			for(int i = itemCapacity - 1; i >= 0; i--)
			{
				var tool = inventory.GetBehavior(i);
				if(tool != null) tool.OnBlockUnloaded();
			}
		}

		protected override MeshData genMesh(ItemStack stack)
		{
			MeshData mesh;
			var dynBlock = stack.Collectible as IContainedMeshSource;

			if(dynBlock != null)
			{
				mesh = dynBlock.GenMesh(stack, capi.BlockTextureAtlas, Pos);
				mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, Block.Shape.rotateY * GameMath.DEG2RAD, 0);
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

			if(stack.Collectible.Attributes?[AttributeTransformCode].Exists == true)
			{
				ModelTransform transform = stack.Collectible.Attributes?[AttributeTransformCode].AsObject<ModelTransform>();
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

		private void UpdateToolBounds(int slotId)
		{
			var tool = inventory.GetBehavior(slotId);
			if(tool == null) selectionBoxes[slotId] = null;
			else
			{
				toolsSelection[slotId] = new SelectionInfo() {
					boxes = GetRotatedBoxes(tool.GetBoundingBoxes(), Block.Shape.rotateY)
				};
			}
		}

		private void RebuildSelectionBoxes()
		{
			var boxes = new List<Cuboidf>();
			if(Block.SelectionBoxes != null) boxes.AddRange(Block.SelectionBoxes);
			else boxes.Add(Block.DefaultCollisionBox);
			blockBoxesCount = boxes.Count;
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