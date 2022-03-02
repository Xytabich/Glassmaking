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
	public class BlockEntityGlassSmeltery : BlockEntity, IBlockEntityContainer, ITimeBasedHeatReceiver, IHeatSourceModifier
	{
		private const double PROCESS_HOURS_PER_UNIT = 0.001;
		private const double BUBBLING_PROCESS_MULTIPLIER = 3;

		private static SimpleParticleProperties smokeParticles;

		float IHeatSourceModifier.FuelRateModifier => 1f;
		float IHeatSourceModifier.TemperatureModifier => 1.1f;

		IInventory IBlockEntityContainer.Inventory => inventory;
		string IBlockEntityContainer.InventoryClassName => inventory.ClassName;

		protected virtual int maxGlassAmount => 1000;

		private BlockRendererGlassSmeltery renderer = null;

		private ITimeBasedHeatSource heatSource = null;

		private SmelteryState state;
		private int glassAmount;
		private AssetLocation glassCode;
		private double processProgress;

		private GlassMakingMod mod;
		private float meltingTemperature;

		private GlassSmelteryInventory inventory = new GlassSmelteryInventory(null, null);

		public override void Initialize(ICoreAPI api)
		{
			mod = api.ModLoader.GetModSystem<GlassMakingMod>();
			base.Initialize(api);
			inventory.LateInitialize("glassmaking:smeltery-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);
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
			if(api.Side == EnumAppSide.Client)
			{
				ICoreClientAPI capi = (ICoreClientAPI)api;
				var bathSource = capi.Tesselator.GetTexSource(Block);
				var bathMesh = ObjectCacheUtil.GetOrCreate(capi, "glassmaking:smeltery-shape-" + Block.Variant["side"], () => {
					var asset = capi.Assets.TryGet(new AssetLocation(Block.Code.Domain, "shapes/block/smeltery/bath.json"));
					capi.Tesselator.TesselateShape("glassmaking:smeltery-shape", asset.ToObject<Shape>(), out var bath, bathSource, new Vec3f(0f, GetRotation(), 0f));
					return capi.Render.UploadMesh(bath);
				});
				renderer = new BlockRendererGlassSmeltery(capi, Pos, EnumRenderStage.Opaque, bathMesh, capi.Tesselator.GetTexSource(Block),
					bathSource["inside"].atlasTextureId, 0.1875f, -0.1875f, 0.625f, 0.625f);
				UpdateRendererFull();
			}
		}

		public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
		{
			base.GetBlockInfo(forPlayer, dsc);
			if(heatSource != null)
			{
				switch(state)
				{
					case SmelteryState.ContainsMix:
						dsc.AppendLine(Lang.Get("glassmaking:Contains {0} units of {1} glass", glassAmount, Lang.Get(GlassBlend.GetBlendNameCode(glassCode))));
						if(heatSource.IsHeatedUp())
						{
							dsc.AppendLine(Lang.Get("Temperature: {0}°C", GetTemperature().ToString("0")));
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
			if(heatSource == null) return false;

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
						if(heatSource.CalcCurrentTemperature() >= meltingTemperature)
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

		public bool CanInteract(EntityAgent byEntity, BlockSelection blockSel)
		{
			return string.Equals(blockSel.Face.Opposite.Code, Block.Variant["side"], StringComparison.OrdinalIgnoreCase);
		}

		public float GetTemperature()
		{
			return heatSource.CalcCurrentTemperature();
		}

		public int GetGlassAmount()
		{
			if(heatSource != null)
			{
				if(state == SmelteryState.ContainsGlass && heatSource.CalcCurrentTemperature() >= (meltingTemperature * 0.9f))
				{
					return glassAmount;
				}
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

		void ITimeBasedHeatReceiver.SetHeatSource(ITimeBasedHeatSource heatSource)
		{
			this.heatSource = heatSource;
		}

		void ITimeBasedHeatReceiver.OnHeatSourceTick(float dt)
		{
			if(state != SmelteryState.Empty && state != SmelteryState.ContainsGlass && heatSource.IsHeatedUp())
			{
				var graph = heatSource.CalcHeatGraph();
				if(state == SmelteryState.ContainsMix)
				{
					if(Api.Side == EnumAppSide.Server && graph.CalculateValueRetention(meltingTemperature) > 0)
					{
						state = SmelteryState.Melting;
						processProgress = 0;
						inventory.Clear();
						UpdateRendererFull();
						MarkDirty(true);
					}
				}
				double timeOffset = 0;
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

			if(Api.Side == EnumAppSide.Client)
			{
				UpdateRendererParameters();
				if(heatSource.IsBurning()) EmitParticles();
			}
		}

		private void EmitParticles()
		{
			if(Api.World.Rand.Next(5) > 0)
			{
				var transform = ((BlockGlassSmeltery)Block).smokeTransform;
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
			renderer.SetParameters(state == SmelteryState.ContainsMix, Math.Min(223, (int)((heatSource == null ? 0 : heatSource.GetTemperature() / 1500f) * 223)));
		}

		private int GetRotation()
		{
			switch(Block.Variant["side"])
			{
				case "north": return 0;
				case "west": return 90;
				case "south": return 180;
				case "east": return 270;
			}
			return 0;
		}

		static BlockEntityGlassSmeltery()
		{
			smokeParticles = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(128, 110, 110, 110), new Vec3d(), new Vec3d(), new Vec3f(-0.2f, 0.3f, -0.2f), new Vec3f(0.2f, 0.3f, 0.2f), 2f, 0f, 0.5f, 1f, EnumParticleModel.Quad);
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