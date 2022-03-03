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

namespace GlassMaking.Blocks
{
	public class BlockEntityLargeSmelteryCore : BlockEntity, IHeatSourceModifier//TODO: structure build stage
	{
		private const double PROCESS_HOURS_PER_UNIT = 0.0005;
		private const double BUBBLING_PROCESS_MULTIPLIER = 3;

		private static readonly ValueGraph defaultGraph = new ValueGraph(new ValueGraph.Point(0, 20));
		private static SimpleParticleProperties smokeParticles;

		public float FuelRateModifier => 0.9f;
		public float TemperatureModifier => 1.5f;

		protected virtual int maxGlassAmount => 10000;

		protected virtual int heatersCount => 4;

		private BlockRendererGlassSmeltery renderer = null;

		private SmelteryState state;
		private int glassAmount;
		private AssetLocation glassCode;
		private double processProgress;

		private GlassMakingMod mod;
		private float meltingTemperature;

		private int tickSource = -1;
		private ITimeBasedHeatSourceControl[] heaters;
		private ValueGraph[] heatGraphs;

		private GlassSmelteryInventory inventory = new GlassSmelteryInventory(null, null);

		public BlockEntityLargeSmelteryCore()
		{
			heaters = new ITimeBasedHeatSourceControl[heatersCount];
			heatGraphs = new ValueGraph[heatersCount];
		}

		public override void Initialize(ICoreAPI api)
		{
			mod = api.ModLoader.GetModSystem<GlassMakingMod>();
			base.Initialize(api);
			inventory.LateInitialize("glassmaking:largesmeltery-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);
			if(state != SmelteryState.Empty)
			{
				var glassType = mod.GetGlassTypeInfo(glassCode);
				if(glassType == null)
				{
					state = SmelteryState.Empty;
					glassCode = null;
					inventory.Clear();
				}
				else
				{
					meltingTemperature = mod.GetGlassTypeInfo(glassCode).meltingPoint;
				}
			}

			var block = (BlockLargeSmeltery)Block;
			for(int i = 0; i < heatersCount; i++)
			{
				(api.World.BlockAccessor.GetBlockEntity(Pos.AddCopy(block.hearthOffsets[i])) as BlockEntityLargeSmelteryHearth)?.OnCoreUpdated(this);
			}

			if(api.Side == EnumAppSide.Client)
			{
				ICoreClientAPI capi = (ICoreClientAPI)api;
				var bathSource = capi.Tesselator.GetTexSource(Block);
				var bathMesh = ObjectCacheUtil.GetOrCreate(capi, "glassmaking:largesmeltery-shape", () => {
					var asset = capi.Assets.TryGet(new AssetLocation(Block.Code.Domain, "shapes/block/largesmeltery/bath.json"));
					capi.Tesselator.TesselateShape("glassmaking:largesmeltery-shape", asset.ToObject<Shape>(), out var bath, bathSource);
					return capi.Render.UploadMesh(bath);
				});
				renderer = new BlockRendererGlassSmeltery(capi, Pos, EnumRenderStage.AfterOIT, bathMesh, capi.Tesselator.GetTexSource(Block),
					bathSource["inside"].atlasTextureId, 0.4375f, 0.6875f, 0.375f, 2f, 0.0001f);
				UpdateRendererFull();
			}

			RegisterGameTickListener(OnCommonTick, 200);
		}

		public void SetHeater(int index, ITimeBasedHeatSourceControl heatSource)
		{
			if(heaters[index] == heatSource) return;

			heaters[index] = heatSource;
			if(tickSource == index)
			{
				if(heatSource != null) return;

				tickSource = -1;
			}
			if(tickSource < 0)
			{
				for(int i = 0; i < heaters.Length; i++)
				{
					if(heaters[i] != null)
					{
						tickSource = i;
						break;
					}
				}
			}
			if(Api.Side == EnumAppSide.Client)
			{
				UpdateRendererParameters();
			}
		}

		public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
		{
			base.GetBlockInfo(forPlayer, dsc);
			switch(state)
			{
				case SmelteryState.ContainsMix:
					dsc.AppendLine(Lang.Get("glassmaking:Contains {0} units of {1} glass", glassAmount, Lang.Get(GlassBlend.GetBlendNameCode(glassCode))));
					float avgTemperature = GetTemperature();
					if(avgTemperature > 25f)
					{
						dsc.AppendLine(Lang.Get("Temperature: {0}°C", avgTemperature.ToString("0")));
					}
					break;
				case SmelteryState.Melting:
					dsc.AppendLine(Lang.Get("glassmaking:Contains {0} units of {1} glass", glassAmount, Lang.Get(GlassBlend.GetBlendNameCode(glassCode))));
					dsc.AppendLine(Lang.Get("glassmaking:Glass melting progress: {0}%", (processProgress / (glassAmount * PROCESS_HOURS_PER_UNIT) * 100).ToString("0")));
					dsc.AppendLine(Lang.Get("Temperature: {0}°C", GetTemperature().ToString("0")));
					break;
				case SmelteryState.Bubbling:
					dsc.AppendLine(Lang.Get("glassmaking:Contains {0} units of {1} glass", glassAmount, Lang.Get(GlassBlend.GetBlendNameCode(glassCode))));
					dsc.AppendLine(Lang.Get("glassmaking:Glass bubbling progress: {0}%", (processProgress / (glassAmount * PROCESS_HOURS_PER_UNIT * BUBBLING_PROCESS_MULTIPLIER) * 100).ToString("0")));
					dsc.AppendLine(Lang.Get("Temperature: {0}°C", GetTemperature().ToString("0")));
					break;
				case SmelteryState.ContainsGlass:
					dsc.AppendLine(Lang.Get("glassmaking:Contains {0} units of molten {1} glass", glassAmount, Lang.Get(GlassBlend.GetBlendNameCode(glassCode))));
					dsc.AppendLine(Lang.Get("Temperature: {0}°C", GetTemperature().ToString("0")));
					break;
			}
		}

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
			base.ToTreeAttributes(tree);
			tree.SetInt("state", (int)state);
			if(state != SmelteryState.Empty)
			{
				tree.SetInt("glassamount", glassAmount);
				tree.SetString("glasscode", glassCode.ToShortString());
				if(state == SmelteryState.ContainsMix)
				{
					inventory.ToTreeAttributes(tree.GetOrAddTreeAttribute("inventory"));
				}
				else if(state != SmelteryState.ContainsGlass)
				{
					tree.SetDouble("progress", processProgress);
				}
			}
		}

		public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
		{
			base.FromTreeAttributes(tree, worldAccessForResolve);
			state = (SmelteryState)tree.GetInt("state");
			if(state != SmelteryState.Empty)
			{
				glassAmount = tree.GetInt("glassamount");
				glassCode = new AssetLocation(tree.GetString("glasscode"));
				if(state == SmelteryState.ContainsMix)
				{
					inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
				}
				else if(state != SmelteryState.ContainsGlass)
				{
					processProgress = tree.GetDouble("progress");
				}
				if(Api?.World != null)
				{
					var glassType = mod.GetGlassTypeInfo(glassCode);
					if(glassType == null)
					{
						state = SmelteryState.Empty;
						glassCode = null;
						inventory.Clear();
					}
					else
					{
						meltingTemperature = mod.GetGlassTypeInfo(glassCode).meltingPoint;
					}
				}
			}
			else
			{
				glassCode = null;
			}
			UpdateRendererFull();
		}

		public override void OnBlockUnloaded()
		{
			base.OnBlockUnloaded();
			renderer?.Dispose();
			var block = (BlockLargeSmeltery)Block;
			for(int i = 0; i < heatersCount; i++)
			{
				(Api.World.BlockAccessor.GetBlockEntity(Pos.AddCopy(block.hearthOffsets[i])) as BlockEntityLargeSmelteryHearth)?.OnCoreUpdated(null);
			}
		}

		public override void OnBlockRemoved()
		{
			renderer?.Dispose();
			base.OnBlockRemoved();
		}

		public void GetGlassFillState(out int canAddAmount, out AssetLocation code)
		{
			code = null;
			canAddAmount = maxGlassAmount;
			if(glassCode != null)
			{
				code = glassCode;
				canAddAmount = maxGlassAmount - glassAmount;
			}
		}

		public bool TryReduceBubbling(double hours)
		{
			if(state == SmelteryState.Bubbling)
			{
				double processDuration = glassAmount * PROCESS_HOURS_PER_UNIT * BUBBLING_PROCESS_MULTIPLIER;
				// The last half hour the melt should "calm down"
				if(processProgress >= processDuration - 0.5) return false;

				processProgress = Math.Min(processProgress + hours, processDuration - 0.5);
				MarkDirty(true);
				return true;
			}
			return false;
		}

		public bool TryAdd(IPlayer byPlayer, ItemSlot slot, int multiplier)
		{
			if(glassAmount >= maxGlassAmount) return false;
			GlassBlend blend = GlassBlend.FromJson(slot.Itemstack);
			if(blend == null) blend = GlassBlend.FromTreeAttributes(slot.Itemstack.Attributes.GetTreeAttribute(GlassBlend.PROPERTY_NAME));
			if(blend != null && blend.Amount > 0 && (blend.Amount + glassAmount) <= maxGlassAmount &&
				(glassCode == null && mod.GetGlassTypeInfo(blend.Code) != null || glassCode.Equals(blend.Code)))
			{
				if(Api.Side == EnumAppSide.Server)
				{
					if(glassCode == null)
					{
						glassCode = blend.Code.Clone();
						meltingTemperature = mod.GetGlassTypeInfo(glassCode).meltingPoint;
					}
					if(state == SmelteryState.Bubbling || state == SmelteryState.ContainsGlass)
					{
						if(GetTemperature() >= meltingTemperature)
						{
							state = SmelteryState.Melting;
							processProgress = PROCESS_HOURS_PER_UNIT * glassAmount;
						}
						else
						{
							state = SmelteryState.ContainsMix;
						}
					}
					int consume = Math.Min(Math.Min(multiplier, slot.Itemstack.StackSize), (maxGlassAmount - glassAmount) / blend.Amount);
					var item = slot.TakeOut(consume);
					if(state == SmelteryState.Empty || state == SmelteryState.ContainsMix)
					{
						inventory.AddItem(item);
						state = SmelteryState.ContainsMix;
					}
					glassAmount += blend.Amount * consume;
					slot.MarkDirty();
					MarkDirty(true);
				}
				return true;
			}
			return false;
		}

		public ItemStack[] GetDropItems()
		{
			var items = inventory.CollectItems();
			if(glassAmount >= 5)
			{
				var shards = new ItemStack(Api.World.GetItem(new AssetLocation("glassmaking", "glassshards")), glassAmount / 5);
				new GlassBlend(glassCode, 5).ToTreeAttributes(shards.Attributes.GetOrAddTreeAttribute(GlassBlend.PROPERTY_NAME));
				items.Add(shards);
			}
			return items.ToArray();
		}

		public float GetTemperature()
		{
			float avgTemperature = 20f;
			for(int i = 0; i < heaters.Length; i++)
			{
				if(heaters[i] != null)
				{
					avgTemperature += heaters[i].CalcCurrentTemperature();
				}
			}
			avgTemperature /= heatersCount;
			return avgTemperature;
		}

		public int GetGlassAmount()
		{
			if(state == SmelteryState.ContainsGlass && GetTemperature() >= (meltingTemperature * 0.9f))
			{
				return glassAmount;
			}
			return 0;
		}

		public AssetLocation GetGlassCode()
		{
			return glassCode;
		}

		public void RemoveGlass(int amount)
		{
			glassAmount -= amount;
			if(glassAmount <= 0 && Api.Side == EnumAppSide.Server)
			{
				state = SmelteryState.Empty;
				glassCode = null;
			}
			MarkDirty(true);
		}

		public void SpawnGlassUseParticles(IWorldAccessor world, BlockSelection blockSel, IPlayer byPlayer, float quantity = 1f)
		{
			// Smoke on the mold
			Vec3d blockpos = Pos.ToVec3d().Add(0.5, 0, 0.5);
			world.SpawnParticles(
				quantity,
				ColorUtil.ToRgba(50, 220, 220, 220),
				blockpos.AddCopy(-0.25, -0.1, -0.25),
				blockpos.Add(0.25, 0.1, 0.25),
				new Vec3f(-0.5f, 0f, -0.5f),
				new Vec3f(0.5f, 0f, 0.5f),
				0.25f,
				-0.05f,
				0.5f,
				EnumParticleModel.Quad,
				byPlayer
			);
		}

		public override void OnLoadCollectibleMappings(IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed)
		{
			base.OnLoadCollectibleMappings(worldForNewMappings, oldBlockIdMapping, oldItemIdMapping, schematicSeed);
			foreach(ItemSlot item in inventory)
			{
				Utils.FixIdMappingOrClear(item, oldBlockIdMapping, oldItemIdMapping, worldForNewMappings);
			}
		}

		public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
		{
			base.OnStoreCollectibleMappings(blockIdMapping, itemIdMapping);
			foreach(ItemSlot slot in inventory)
			{
				Utils.StoreCollectibleMappings(slot, blockIdMapping, itemIdMapping, Api.World);
			}
		}

		private void OnCommonTick(float dt)
		{
			if(state != SmelteryState.Empty && state != SmelteryState.ContainsGlass)
			{
				bool hasActive = false;
				for(int i = 0; i < heaters.Length; i++)
				{
					if(heaters[i] != null && (heaters[i].GetTemperature() > 25 || heaters[i].IsBurning()))
					{
						hasActive = true;
						heatGraphs[i] = heaters[i].CalcHeatGraph();
					}
					else
					{
						heatGraphs[i] = defaultGraph;
					}
				}
				if(hasActive)
				{
					var graph = ValueGraph.Avg(heatGraphs);
					double timeOffset = 0;
					if(state == SmelteryState.ContainsMix)
					{
						if(Api.Side == EnumAppSide.Server && graph.CalculateValueRetention(timeOffset, meltingTemperature) > 0)
						{
							state = SmelteryState.Melting;
							processProgress = 0;
							inventory.Clear();
							UpdateRendererFull();
							MarkDirty(true);
						}
					}
					if(state == SmelteryState.Melting)
					{
						double timeLeft = glassAmount * PROCESS_HOURS_PER_UNIT - processProgress;
						double time = graph.CalculateValueRetention(timeOffset, meltingTemperature);
						processProgress += Math.Min(time, timeLeft);
						if(Api.Side == EnumAppSide.Server && time >= timeLeft)
						{
							timeOffset += timeLeft;
							processProgress = 0;
							state = SmelteryState.Bubbling;
							MarkDirty(true);
						}
					}
					if(state == SmelteryState.Bubbling)
					{
						double timeLeft = glassAmount * PROCESS_HOURS_PER_UNIT * BUBBLING_PROCESS_MULTIPLIER - processProgress;
						double time = graph.CalculateValueRetention(timeOffset, meltingTemperature * 1.11f);
						processProgress += Math.Min(time, timeLeft);
						if(Api.Side == EnumAppSide.Server && time >= timeLeft)
						{
							state = SmelteryState.ContainsGlass;
							MarkDirty(true);
						}
					}
				}
			}

			if(Api.Side == EnumAppSide.Client)
			{
				UpdateRendererParameters();
				for(int i = 0; i < heaters.Length; i++)
				{
					if(heaters[i] != null && heaters[i].IsBurning())
					{
						EmitParticles();
						break;
					}
				}
			}

			for(int i = 0; i < heaters.Length; i++)
			{
				heaters[i]?.OnTick(dt);
			}
		}

		private void EmitParticles()
		{
			if(Api.World.Rand.Next(3) > 0)
			{
				var transform = ((BlockLargeSmeltery)Block).smokeTransform;
				smokeParticles.MinPos.Set(Pos.X + transform.Translation.X, Pos.Y + transform.Translation.Y, Pos.Z + transform.Translation.Z);
				smokeParticles.AddPos.Set(transform.ScaleXYZ.X, 0.0, transform.ScaleXYZ.Z);
				Api.World.SpawnParticles(smokeParticles);
			}
		}

		private void UpdateRendererFull()
		{
			if(Api != null && Api.Side == EnumAppSide.Client && renderer != null)
			{
				renderer.SetHeight((float)glassAmount / maxGlassAmount);
				UpdateRendererParameters();
			}
		}

		private void UpdateRendererParameters()
		{
			renderer.SetParameters(state == SmelteryState.ContainsMix, Math.Min(223, (int)((GetTemperature() / 3000f) * 191)));
		}

		static BlockEntityLargeSmelteryCore()
		{
			smokeParticles = new SimpleParticleProperties(1f, 3f, ColorUtil.ToRgba(128, 110, 110, 110), new Vec3d(), new Vec3d(), new Vec3f(-0.2f, 0.3f, -0.2f), new Vec3f(0.2f, 0.3f, 0.2f), 2f, 0f, 0.5f, 1f, EnumParticleModel.Quad);
			smokeParticles.SelfPropelled = true;
			smokeParticles.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -255f);
			smokeParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 2f);
		}

		private enum SmelteryState
		{
			Empty,
			ContainsMix,
			Melting,
			Bubbling,
			ContainsGlass
		}
	}
}