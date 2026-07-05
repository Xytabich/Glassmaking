using GlassMaking.Common;
using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GlassMaking.Blocks
{
	public class BlockEntityAnnealer : BlockEntityDisplay, ITimeBasedHeatReceiver
	{
		private static SimpleParticleProperties smokeParticles;

		public override InventoryBase Inventory => inventory;
		public override string InventoryClassName => "glassmaking:annealer";
		public override string AttributeTransformCode => "annealerTransform";

		protected virtual int ItemCapacity => 9;

		private readonly InventoryGeneric inventory;
		private ItemStack? lastRemoved = null;

		private ITimeBasedHeatSourceControl? heatSource = null;
		private readonly ItemProcessInfo?[] processes;

		// gridSize = columns per row; gridRows = number of rows.
		// Both are 0 when the annealer is empty.
		private int gridSize = 0;
		private int gridRows = 0;
		private float gridCellSize;

		private bool preventMeshUpdate = false;
		private volatile AnnealerGlowRenderer? glowRenderer = null;

		/// <summary>Number of inventory slots that the glow renderer should cover.</summary>
		public int GlowItemCount => ItemCapacity;
		/// <summary>Per-slot transformation matrices (pure translation). Null until first tessellation.</summary>
		public float[][]? GlowTfMatrices => tfMatrices;
		/// <summary>Returns the cached mesh for a slot, or null if not yet tessellated.</summary>
		public MeshData? GetSlotMesh(ItemSlot slot) => getMesh(slot);

		public BlockEntityAnnealer()
		{
			inventory = new InventoryGeneric(ItemCapacity, InventoryClassName + "-" + Pos, null);
			for(int i = ItemCapacity - 1; i >= 0; i--)
			{
				inventory[i].MaxSlotStackSize = 1;
			}
			processes = new ItemProcessInfo[ItemCapacity];
		}

		public override void Initialize(ICoreAPI api)
		{
			preventMeshUpdate = true;
			base.Initialize(api);
			preventMeshUpdate = false;
			for(int i = 0; i < processes.Length; i++)
			{
				if(processes[i] != null) ResolveProcessInfo(i);
			}
			UpdateGrid();
			if(Api.Side == EnumAppSide.Client) updateMeshes();
			RegisterGameTickListener(OnCommonTick, 1000);
		}

		public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
		{
			if(!inventory.Empty)
			{
				dsc.AppendLine(Lang.Get("Contents:"));
				for(int i = 0; i < ItemCapacity; i++)
				{
					if(!inventory[i].Empty)
					{
						dsc.Append(inventory[i].GetStackName());
						float temperature = (inventory[i].Itemstack!.Attributes["temperature"] as ITreeAttribute)?.GetFloat("temperature", 20f) ?? 20f;
						dsc.Append("  ").AppendLine(Lang.Get("Temperature: {0}°C", temperature.ToString("0")));
						var process = processes[i];
						if(process != null && process.IsHeated)
						{
							dsc.AppendLine(Lang.Get("glassmaking:Annealing: {0}", (Math.Min(process.Time / process.AnnealTime, 1) * 100).ToString("0")));
						}
					}
				}
			}
		}

		public bool TryInteract(IPlayer byPlayer, ItemSlot slot)
		{
			if(slot.Empty || slot.Itemstack.Equals(Api.World, lastRemoved, GlobalConstants.IgnoredStackAttributes))
			{
				bool removed = false;
				for(int i = 0; i < processes.Length; i++)
				{
					if(!inventory[i].Empty && (processes[i] == null || byPlayer.Entity.Controls.Sneak))
					{
						inventory[i].TryPutInto(Api.World, slot, 1);
						lastRemoved = slot.Itemstack!.Clone();
						processes[i] = null;
						removed = true;
						break;
					}
				}
				if(removed)
				{
					if(inventory.Empty)
					{
						gridSize = 0;
						gridRows = 0;
						lastRemoved = null;
					}
					if(Api.Side == EnumAppSide.Client) updateMeshes();
					MarkDirty(true);
					return true;
				}
				lastRemoved = null;
				return false;
			}
			else
			{
				var properties = slot.Itemstack.Collectible.Attributes?["glassmaking:anneal"];
				if(properties != null && properties.Exists)
				{
					if(gridSize > 0)
					{
						float size = slot.Itemstack.Collectible.Attributes?["annealerSize"].AsFloat(1f) ?? 1f;
						if(size > gridCellSize) return false;

						int len = gridSize * gridRows;
						for(int i = 0; i < len; i++)
						{
							if(inventory[i].Empty)
							{
								inventory[i].Itemstack = slot.TakeOut(1);
								lastRemoved = null;
								processes[i] = new ItemProcessInfo() { IsHeated = false, Time = 0 };
								ResolveProcessInfo(i);
								if(Api.Side == EnumAppSide.Client) updateMeshes();
								MarkDirty(true);
								return true;
							}
						}
					}
					else
					{
						float size = slot.Itemstack.Collectible.Attributes?["annealerSize"].AsFloat(1f) ?? 1f;
						if(size > 1) return false;
						SetGridForSize(size);
						inventory[0].Itemstack = slot.TakeOut(1);
						lastRemoved = null;
						processes[0] = new ItemProcessInfo() { IsHeated = false, Time = 0 };
						ResolveProcessInfo(0);
						if(Api.Side == EnumAppSide.Client) updateMeshes();
						MarkDirty(true);
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>Sets gridSize, gridRows, and gridCellSize for the given item size value.</summary>
		private void SetGridForSize(float size)
		{
			if(size <= 0.16f)
			{
				// 6-column × 1-row layout — up to 6 items
				gridCellSize = 1f / 6f;
				gridSize = 6;
				gridRows = 1;
			}
			else if(size <= 0.25f)
			{
				// 4-column × 2-row layout — up to 8 items
				gridCellSize = 0.25f;
				gridSize = 4;
				gridRows = 2;
			}
			else if(size <= 1f / 3f)
			{
				gridCellSize = 1f / 3f;
				gridSize = 3;
				gridRows = 3;
			}
			else if(size <= 0.5f)
			{
				gridCellSize = 0.5f;
				gridSize = 2;
				gridRows = 2;
			}
			else
			{
				gridCellSize = 1f;
				gridSize = 1;
				gridRows = 1;
			}
		}

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
			base.ToTreeAttributes(tree);
			inventory.ToTreeAttributes(tree);
			tree.SetInt("gridRows", gridRows);
			for(int i = 0; i < processes.Length; i++)
			{
				var process = processes[i];
				if(!inventory[i].Empty && process != null)
				{
					var attrib = tree.GetOrAddTreeAttribute("process" + i);
					attrib.SetBool("isHeated", process.IsHeated);
					attrib.SetDouble("time", process.Time);
				}
			}
		}

		public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
		{
			preventMeshUpdate = true;
			base.FromTreeAttributes(tree, worldForResolving);
			preventMeshUpdate = false;
			gridRows = tree.GetInt("gridRows", 0);
			for(int i = 0; i < processes.Length; i++)
			{
				var attrib = tree.GetTreeAttribute("process" + i);
				if(attrib != null)
				{
					processes[i] = new ItemProcessInfo() {
						IsHeated = attrib.GetBool("isHeated"),
						Time = attrib.GetDouble("time")
					};
				}
				else
				{
					processes[i] = null;
				}
			}
			if(Api?.World != null)
			{
				for(int i = 0; i < processes.Length; i++)
				{
					if(processes[i] != null) ResolveProcessInfo(i);
				}
				UpdateGrid();
				if(Api.Side == EnumAppSide.Client) updateMeshes();
			}
		}

		protected override float[][] genTransformationMatrices()
		{
			int len = DisplayedItems;
			float[][] tfMatrices = new float[len][];
			var tmpMat = new Matrixf();
			var transform = ((BlockAnnealer)Block).ContentTransform;
			for(int i = 0; i < len; i++)
			{
				tmpMat.Identity();
				if(gridSize != 0)
				{
					int col = i % gridSize;
					int row = i / gridSize;

					tmpMat.Translate(
						transform.Translation.X + (col + 0.5f) / gridSize * transform.ScaleXYZ.X,
						transform.Translation.Y,
						transform.Translation.Z + (row + 0.5f) / gridRows * transform.ScaleXYZ.Z);
				}
				tfMatrices[i] = (float[])tmpMat.Values.Clone();
			}
			return tfMatrices;
		}

		public override void updateMeshes()
		{
			if(preventMeshUpdate) return;
			base.updateMeshes();

			if(Api?.Side != EnumAppSide.Client) return;

			bool anyHot = false;
			for(int i = 0; i < ItemCapacity; i++)
			{
				var slot = inventory[i];
				if(!slot.Empty)
				{
					float temp = (slot.Itemstack.Attributes["temperature"] as ITreeAttribute)
						?.GetFloat("temperature", 20f) ?? 20f;
					if(temp >= 500f) { anyHot = true; break; }
				}
			}

			if(anyHot)
			{
				if(glowRenderer == null)
					glowRenderer = new AnnealerGlowRenderer((ICoreClientAPI)Api, this);
				else
					glowRenderer.UpdateMeshRefs();
			}
			else
			{
				glowRenderer?.Dispose();
				glowRenderer = null;
			}
		}

		public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
		{
			// When the glow renderer is active it draws all items per-frame with incandescence
			// uniforms, so we skip adding them to the chunk mesh here. The block model itself
			// still tessellates normally (return false, not true).
			if(glowRenderer != null) return false;

			return base.OnTesselation(mesher, tessThreadTesselator);
		}

		public override void OnBlockRemoved()
		{
			base.OnBlockRemoved();
			glowRenderer?.Dispose();
			glowRenderer = null;
		}

		public override void OnBlockUnloaded()
		{
			base.OnBlockUnloaded();
			glowRenderer?.Dispose();
			glowRenderer = null;
		}

		void ITimeBasedHeatReceiver.SetHeatSource(ITimeBasedHeatSourceControl? heatSource)
		{
			this.heatSource = heatSource;
		}

		private void OnCommonTick(float dt)
		{
			// Runs before the heatSource guard; raw glass can shatter even with no heat source attached.
			if(Api.Side == EnumAppSide.Server) ShatterCooledItems();

			if(heatSource == null) return;

			bool anyHot = false;

			if(gridSize != 0)
			{
				var totalHours = Api.World.Calendar.TotalHours;
				var graph = heatSource.CalcHeatGraph();
				for(int i = 0; i < ItemCapacity; i++)
				{
					var slot = inventory[i];
					if(!slot.Empty)
					{
						float temperature = (slot.Itemstack.Attributes["temperature"] as ITreeAttribute)?.GetFloat("temperature", 20f) ?? 20f;
						var process = processes[i];
						if(process != null)
						{
							double timeOffset = 0;
							if(!process.IsHeated)
							{
								if(Api.Side == EnumAppSide.Server)
								{
									// Only start annealing when the item is within the valid temperature range.
									// A too-hot item must cool down to [Min, Max] first.
									double? time;
									if(temperature >= process.AnnealTemperature.Min && temperature <= process.AnnealTemperature.Max)
										time = 0;
									else if(temperature < process.AnnealTemperature.Min)
										time = graph.ReachValue(temperature, process.AnnealTemperature.Min, 1000f, 90f);
									else
										time = null; // too hot — wait for it to cool
									if(time.HasValue)
									{
										process.IsHeated = true;
										timeOffset = time.Value;
										MarkDirty(true);
									}
								}
							}
							if(process.IsHeated)
							{
								// Only accumulate annealing time while temperature stays within [Min, Max].
								if(temperature >= process.AnnealTemperature.Min && temperature <= process.AnnealTemperature.Max)
								{
									process.Time += Math.Max(0, Math.Min((temperature - process.AnnealTemperature.Min) / 90f, totalHours - heatSource.GetLastTickTime()) - timeOffset);
								}
								if(process.Time >= process.AnnealTime && Api.Side == EnumAppSide.Server)
								{
									processes[i] = null;
									slot.Itemstack = process.Output.Clone();
									MarkDirty(true);
								}
							}
						}
						temperature = Math.Max((float)graph.CalculateFinalValue(temperature, 1000f, 270f), 20f);
						slot.Itemstack.Collectible.SetTemperature(Api.World, slot.Itemstack, temperature);

						if(temperature > 200f) anyHot = true;
					}
				}
			}

			if(Api.Side == EnumAppSide.Client)
			{
				if(heatSource.IsBurning()) EmitParticles();
			}
			else if(anyHot)
			{
				// Sync temperatures to clients every tick so they can show the correct glow.
				MarkDirty(true);
			}

			heatSource.OnTick(dt);
		}

		/// <summary>
		/// Breaks any raw glass that has cooled below the shatter threshold.
		/// Shards are dropped as item entities because a slot cannot hold the original item and new shards simultaneously.
		/// </summary>
		private void ShatterCooledItems()
		{
			bool changed = false;
			for(int i = 0; i < ItemCapacity; i++)
			{
				var slot = inventory[i];
				if(slot.Empty) continue;
				var stack = slot.Itemstack;
				if(!GlassShatter.ShouldShatter(Api.World, stack)) continue;

				slot.Itemstack = null;
				processes[i] = null;
				changed = true;

				var spawnPos = new Vec3d(Pos.X + 0.5, Pos.Y + 0.5, Pos.Z + 0.5);
				foreach(var shardStack in GlassShatter.CreateShardStacks(Api.World, stack))
				{
					Api.World.SpawnItemEntity(shardStack, spawnPos);
				}
				Api.World.PlaySoundAt(new AssetLocation("game", "sounds/block/glass"), Pos.X + 0.5, Pos.Y + 0.5, Pos.Z + 0.5, null, true, 16f);
			}

			if(changed)
			{
				UpdateGrid();
				MarkDirty(true);
			}
		}

		private void ResolveProcessInfo(int index)
		{
			var process = processes[index];
			if(process == null) return;

			var stack = inventory[index].Itemstack!;
			var properties = stack.Collectible.Attributes?["glassmaking:anneal"];
			if(properties != null && properties.Exists)
			{
				var output = properties["output"].AsObject<JsonItemStack?>(null, stack.Collectible.Code.Domain);
				if(output!.Resolve(Api.World, "annealer"))
				{
					process.AnnealTemperature = properties["temperature"].AsObject<MinMaxFloat>()!;
					process.AnnealTime = properties["time"].AsInt() / 3600.0;
					process.Output = output.ResolvedItemStack!;
					return;
				}
			}
			processes[index] = null;
		}

		private void UpdateGrid()
		{
			float maxSize = 0f;
			int itemsCount = 0;
			for(int i = 0; i < ItemCapacity; i++)
			{
				var slot = inventory[i];
				if(!slot.Empty)
				{
					itemsCount++;
					maxSize = Math.Max(maxSize, slot.Itemstack.ItemAttributes?["annealerSize"].AsFloat(1f) ?? 1f);
				}
			}
			if(itemsCount > 0 && gridSize == 0)
			{
				int maxSlots = itemsCount > 4 ? 8 : (itemsCount > 1 ? 4 : 1);
				maxSize = Math.Min(maxSize, maxSlots >= 8 ? 0.25f : (maxSlots >= 4 ? 0.5f : 1f));
				SetGridForSize(maxSize);
			}
			if(itemsCount == 0)
			{
				gridSize = 0;
				gridRows = 0;
			}
		}

		private void EmitParticles()
		{
			if(Api.World.Rand.Next(5) > 0)
			{
				var transform = ((BlockAnnealer)Block).SmokeTransform;
				smokeParticles.MinPos.Set(Pos.X + transform.Translation.X, Pos.Y + transform.Translation.Y, Pos.Z + transform.Translation.Z);
				smokeParticles.AddPos.Set(transform.ScaleXYZ.X, 0.0, transform.ScaleXYZ.Z);
				Api.World.SpawnParticles(smokeParticles);
			}
		}

		static BlockEntityAnnealer()
		{
			smokeParticles = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(128, 110, 110, 110), new Vec3d(), new Vec3d(), new Vec3f(-0.2f, 0.3f, -0.2f), new Vec3f(0.2f, 0.3f, 0.2f), 2f, 0f, 0.5f, 1f, EnumParticleModel.Quad);
			smokeParticles.SelfPropelled = true;
			smokeParticles.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -255f);
			smokeParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 2f);
		}

		private class ItemProcessInfo
		{
			public MinMaxFloat AnnealTemperature = default!;
			public double AnnealTime;
			public ItemStack Output = default!;
			public bool IsHeated;
			public double Time;
		}
	}
}
