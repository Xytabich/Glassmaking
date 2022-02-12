using GlassMaking.Common;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GlassMaking.Blocks
{
	public class BlockEntityFirebox : BlockEntity, ITimeBasedHeatSource, IHeatSource
	{
		private const float TEMP_INCREASE_PER_HOUR = 1500;
		private const float TEMP_DECREASE_PER_HOUR = 2000;

		private static SimpleParticleProperties smokeParticles;
		private static AdvancedParticleProperties fireParticles;

		protected virtual int maxFuelCount => 16;

		private ItemStack contents => inventory[0].Itemstack;
		private ItemSlot contentsSlot => inventory[0];
		private InventoryGeneric inventory = new InventoryGeneric(1, "firebox-1", null);

		private BlockRendererFirebox renderer = null;

		private ITimeBasedHeatReceiver receiver = null;
		private IBurnerModifier modifier = null;

		private ILoadedSound ambientSound = null;

		private bool burning = false;
		private float temperature = 20;
		/// <summary>
		/// How much burning time is left
		/// </summary>
		private float fuelLevel = 0f;
		// Fuel parameters
		private float fuelTemperature = 0f;

		private float _fuelBurnDuration = 0f;
		private float fuelBurnDuration => _fuelBurnDuration * durationModifier;

		private float temperatureModifier => modifier == null ? 1f : modifier.TemperatureModifier;
		private float durationModifier => modifier == null ? 1f : modifier.DurationModifier;

		private double lastTickTime;

		public override void Initialize(ICoreAPI api)
		{
			base.Initialize(api);
			inventory.LateInitialize("firebox-1", api);
			if(contents != null) ApplyFuelParameters();
			SetReceiver(api.World.BlockAccessor.GetBlockEntity(Pos.UpCopy()) as ITimeBasedHeatReceiver);
			if(api.Side == EnumAppSide.Client)
			{
				ICoreClientAPI capi = (ICoreClientAPI)api;
				renderer = new BlockRendererFirebox(Pos, capi.Tesselator.GetTexSource(Block), capi);
				capi.Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "glassmaking:firebox");
				UpdateRendererFull();
				ToggleAmbientSounds(burning);
			}
			RegisterGameTickListener(OnCommonTick, 200);
		}

		public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
		{
			base.GetBlockInfo(forPlayer, dsc);
			if(contents != null)
			{
				if(contents.StackSize > 0)
				{
					dsc.AppendLine(Lang.Get("Contents: {0}x{1}", contents.StackSize, contents.GetName()));
				}
				else
				{
					dsc.AppendLine(Lang.Get("glassmaking:Fuel type: {0}", contents.GetName()));
				}
			}
			if(burning)
			{
				var calendar = Api.World.Calendar;
				dsc.AppendLine(Lang.Get("Burn duration: {0}s", ((fuelLevel + GetFuelCount() * fuelBurnDuration) * 3600 / (calendar.SpeedOfTime * calendar.CalendarSpeedMul)).ToString("0")));
			}
		}

		public override void OnBlockUnloaded()
		{
			base.OnBlockUnloaded();
			renderer?.Dispose();
			ambientSound?.Dispose();
		}

		public override void OnBlockRemoved()
		{
			renderer?.Dispose();
			ambientSound?.Dispose();
			base.OnBlockRemoved();
		}

		public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
		{
			base.FromTreeAttributes(tree, worldAccessForResolve);
			inventory.FromTreeAttributes(tree);
			fuelLevel = tree.GetFloat("fuelLevel");
			temperature = tree.GetFloat("temperature", 20);
			burning = tree.GetBool("burning");
			lastTickTime = tree.GetDouble("lastTickTotalHours");
			if(contents != null && Api?.World != null) ApplyFuelParameters();
			UpdateRendererFull();
			ToggleAmbientSounds(burning);
		}

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
			base.ToTreeAttributes(tree);
			inventory.ToTreeAttributes(tree);
			tree.SetFloat("fuelLevel", fuelLevel);
			tree.SetFloat("temperature", temperature);
			tree.SetBool("burning", burning);
			tree.SetDouble("lastTickTotalHours", lastTickTime);
		}

		public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
		{
			return fuelTemperature > 20f ? Math.Max((temperature - 20f) / (fuelTemperature - 20f) * 8f, 0f) : 0f;
		}

		public void SetReceiver(ITimeBasedHeatReceiver receiver)
		{
			if(this.receiver != receiver)
			{
				if(this.receiver != null) this.receiver.SetHeatSource(null);
				this.receiver = receiver;
				modifier = receiver as IBurnerModifier;
				if(receiver != null) receiver.SetHeatSource(this);
			}
		}

		public ItemStack[] GetDropItems()
		{
			if(contentsSlot.StackSize < 1) return null;
			return new ItemStack[] { contents.Clone() };
		}

		public void GetFuelStackState(out int canAddAmount, out ItemStack stack)
		{
			stack = null;
			canAddAmount = maxFuelCount;
			if(contents != null)
			{
				canAddAmount = maxFuelCount - GetFuelCount();
				stack = contents;
			}
		}

		public bool TryAdd(IPlayer byPlayer, ItemSlot slot, int count)
		{
			var combustibleProps = slot.Itemstack.Collectible.CombustibleProps;
			if(combustibleProps == null || combustibleProps.BurnTemperature < 100) return false;

			int consume = Math.Min(maxFuelCount - GetFuelCount(), Math.Min(slot.Itemstack.StackSize, count));
			if(consume > 0)
			{
				if(slot.TryPutInto(byPlayer.Entity.World, contentsSlot, consume) > 0)
				{
					if(Api.Side == EnumAppSide.Server)
					{
						if(!burning && temperature * temperatureModifier >= 300)
						{
							burning = true;
							ApplyFuelParameters();
							fuelLevel = fuelBurnDuration;
							contents.StackSize--;
						}
						slot.MarkDirty();
						MarkDirty(true);
					}
					UpdateRendererFull();
					return true;
				}
			}
			return false;
		}

		public bool CanIgnite()
		{
			return !burning && GetFuelCount() > 0;
		}

		public void TryIgnite()
		{
			if(Api.Side == EnumAppSide.Server)
			{
				burning = true;
				ApplyFuelParameters();
				fuelLevel = fuelBurnDuration;
				if(contentsSlot.StackSize > 0) contents.StackSize--;
				lastTickTime = Api.World.Calendar.TotalHours;
				UpdateRendererFull();
				MarkDirty(true);
			}
		}

		public bool IsBurning()
		{
			return burning;
		}

		public bool IsHeatedUp()
		{
			return CalcCurrentTemperature() > 25 * temperatureModifier;
		}

		public float GetTemperature()
		{
			return temperature * temperatureModifier;
		}

		public double GetLastTickTime()
		{
			double totalHours = Api.World.Calendar.TotalHours;
			return lastTickTime > totalHours ? totalHours : lastTickTime;
		}

		public HeatGraph CalcHeatGraph(double totalHours = 0)
		{
			HeatGraph graph = default;

			float temp = temperature;
			graph.StartTemperature = temp;
			graph.WorkingTemperature = temp;

			if(totalHours <= 0) totalHours = Api.World.Calendar.TotalHours;
			double hours = totalHours - lastTickTime;
			graph.TotalTime = Math.Max(hours, 0);

			if(burning && hours > 0)
			{
				double burnTime = fuelLevel + fuelBurnDuration * GetFuelCount();
				if(temp < fuelTemperature)
				{
					double time = Math.Min((fuelTemperature - temp) / TEMP_INCREASE_PER_HOUR, Math.Min(hours, burnTime));
					temp += (float)(time * TEMP_INCREASE_PER_HOUR);
					hours -= time;
					burnTime -= time;
					graph.TransitionTime = time;
					graph.WorkingTemperature = temp;
				}
				else if(temp > fuelTemperature)
				{
					graph.TransitionTime = Math.Min((temp - fuelTemperature) / TEMP_DECREASE_PER_HOUR, hours);
					temp -= (float)(graph.TransitionTime * TEMP_DECREASE_PER_HOUR);
					graph.WorkingTemperature = temp;
				}
				if(hours > 0)
				{
					graph.HoldTime = Math.Min(burnTime, hours);
					hours -= graph.HoldTime;
				}
			}
			if(hours > 0)
			{
				graph.CoolingTime = Math.Min((temp - 20) / TEMP_DECREASE_PER_HOUR, hours);
				temp -= (float)(graph.CoolingTime * TEMP_DECREASE_PER_HOUR);
			}
			graph.EndTemperature = temp;

			return graph.MultiplyTemperature(temperatureModifier);
		}

		public float CalcCurrentTemperature(double totalHours = 0)
		{
			float temp = temperature;
			if(totalHours <= 0) totalHours = Api.World.Calendar.TotalHours;
			double hours = totalHours - lastTickTime;
			if(burning && hours > 0)
			{
				double burnTime = fuelLevel + fuelBurnDuration * GetFuelCount();
				if(temp < fuelTemperature)
				{
					double time = Math.Min((fuelTemperature - temp) / TEMP_INCREASE_PER_HOUR, Math.Min(hours, burnTime));
					temp += (float)(time * TEMP_INCREASE_PER_HOUR);
					hours -= time;
					burnTime -= time;
				}
				else if(temp > fuelTemperature)
				{
					temp = Math.Max(fuelTemperature, temp - (float)(hours * TEMP_DECREASE_PER_HOUR));
				}
				if(hours > 0)
				{
					hours -= Math.Min(burnTime, hours);
				}
			}
			if(hours > 0)
			{
				temp -= (float)(Math.Min((temp - 20) / TEMP_DECREASE_PER_HOUR, hours) * TEMP_DECREASE_PER_HOUR);
			}
			return temp * temperatureModifier;
		}

		public override void OnLoadCollectibleMappings(IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed)
		{
			base.OnLoadCollectibleMappings(worldForNewMappings, oldBlockIdMapping, oldItemIdMapping, schematicSeed);
			Utils.FixIdMappingOrClear(contentsSlot, oldBlockIdMapping, oldItemIdMapping, worldForNewMappings);
			if(contents == null) burning = false;
		}

		public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
		{
			base.OnStoreCollectibleMappings(blockIdMapping, itemIdMapping);
			contents?.Collectible.OnStoreCollectibleMappings(Api.World, contentsSlot, blockIdMapping, itemIdMapping);
		}

		private void OnCommonTick(float dt)
		{
			double totalHours = Api.World.Calendar.TotalHours;
			if(receiver != null) receiver.OnHeatSourceTick(dt);
			if(totalHours < lastTickTime) lastTickTime = totalHours;
			if(burning && totalHours > lastTickTime)
			{
				double hours = totalHours - GetLastTickTime();
				int fuelCount = GetFuelCount();
				double burnTime = fuelLevel + fuelBurnDuration * fuelCount;
				if(temperature < fuelTemperature)
				{
					double time = Math.Min((fuelTemperature - temperature) / TEMP_INCREASE_PER_HOUR, Math.Min(hours, burnTime));
					temperature += (float)(time * TEMP_INCREASE_PER_HOUR);
					hours -= time;
					burnTime -= time;
					lastTickTime += time;
				}
				else if(temperature > fuelTemperature)
				{
					temperature = Math.Max(fuelTemperature, temperature - (float)(hours * TEMP_DECREASE_PER_HOUR));
				}
				if(hours > 0)
				{
					var time = Math.Min(burnTime, hours);
					burnTime -= time;
					lastTickTime += time;
				}
				if(fuelCount > 0)
				{
					int usedFuelCount = fuelCount - (int)Math.Floor(burnTime / fuelBurnDuration);
					fuelLevel = (float)(burnTime % fuelBurnDuration);
					if(usedFuelCount > 0 && Api.Side == EnumAppSide.Server)
					{
						contents.StackSize -= usedFuelCount;
						if(contentsSlot.StackSize <= 0)
						{
							burning = fuelLevel > 0;
							if(!burning)
							{
								contentsSlot.Itemstack = null;
							}
						}
						MarkDirty(true);
					}
				}
				else
				{
					fuelLevel = (float)burnTime;
					burning = fuelLevel > 0;
					if(!burning && Api.Side == EnumAppSide.Server)
					{
						contentsSlot.Itemstack = null;
						MarkDirty(true);
					}
				}
			}
			if(!burning && temperature > 20)
			{
				double hours = totalHours - lastTickTime;
				temperature = Math.Max(20, temperature - (float)(hours * TEMP_DECREASE_PER_HOUR));
			}
			lastTickTime = totalHours;

			if(Api.Side == EnumAppSide.Client)
			{
				UpdateRendererParameters();
				if(burning) EmitParticles();
			}
		}

		private int GetFuelCount()
		{
			return contentsSlot.StackSize;
		}

		private void ApplyFuelParameters()
		{
			var combustibleProps = contents.Collectible.CombustibleProps;//TODO: smoke level?
			fuelTemperature = combustibleProps.BurnTemperature;
			var calendar = Api.World.Calendar;
			_fuelBurnDuration = combustibleProps.BurnDuration * calendar.SpeedOfTime * calendar.CalendarSpeedMul;
			_fuelBurnDuration *= 1f / 3600f;
		}

		private void UpdateRendererFull()
		{
			if(Api != null && Api.Side == EnumAppSide.Client && renderer != null)
			{
				UpdateRendererParameters();
				renderer.SetHeight(GetFuelHeight());
			}
		}

		private int GetFuelHeight()
		{
			return GetFuelCount() + (burning ? 1 : 0);
		}

		private void UpdateRendererParameters()
		{
			renderer.SetParameters(burning, Math.Min(128, (int)((temperature * temperatureModifier / 1500f) * 128)));
		}

		private void EmitParticles()
		{
			if(Api.World.GetLockFreeBlockAccessor().GetBlockId(Pos.X, Pos.Y + 1, Pos.Z) == 0)
			{
				double fuelOffset = GetFuelHeight() / 24.0 + 1.0 / 16;
				smokeParticles.MinPos.Set(Pos.X + 0.5 - 0.3125, Pos.Y + fuelOffset, Pos.Z + 0.5 - 0.3125);
				fireParticles.basePos.Set(Pos.X + 0.5, Pos.Y + fuelOffset, Pos.Z + 0.5);
				Api.World.SpawnParticles(smokeParticles);
				Api.World.SpawnParticles(fireParticles);
			}
		}

		private void ToggleAmbientSounds(bool on)
		{
			if(Api?.World == null || Api.Side != EnumAppSide.Client) return;
			if(on)
			{
				if(ambientSound == null || !ambientSound.IsPlaying)
				{
					ambientSound = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams {
						Location = new AssetLocation("sounds/environment/fireplace.ogg"),
						ShouldLoop = true,
						Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
						DisposeOnFinish = false,
						Volume = 1f
					});
					ambientSound.Start();
				}
			}
			else if(ambientSound != null)
			{
				ambientSound.Stop();
				ambientSound.Dispose();
				ambientSound = null;
			}
		}

		static BlockEntityFirebox()
		{
			smokeParticles = new SimpleParticleProperties(1f, 2.5f, ColorUtil.ToRgba(150, 80, 80, 80), new Vec3d(), new Vec3d(0.75, 0.0, 0.75), new Vec3f(-0.03125f, 0.1f, -0.03125f), new Vec3f(0.03125f, 0.5f, 0.03125f), 2f, -0.00625f, 0.2f, 1f, EnumParticleModel.Quad);
			smokeParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.25f);
			smokeParticles.SelfPropelled = true;
			smokeParticles.WindAffected = true;
			smokeParticles.WindAffectednes = 0.3f;
			smokeParticles.AddPos.Set(0.625, 0.0, 0.625);
			fireParticles = new AdvancedParticleProperties();
			fireParticles.HsvaColor = new NatFloat[] { new NatFloat(20, 20, EnumDistribution.UNIFORM), new NatFloat(255, 50, EnumDistribution.UNIFORM), new NatFloat(255, 50, EnumDistribution.UNIFORM), new NatFloat(255, 0, EnumDistribution.UNIFORM) };
			fireParticles.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.QUADRATIC, -16f);
			fireParticles.GravityEffect = new NatFloat(-0.01f, 0, EnumDistribution.UNIFORM);
			fireParticles.PosOffset = new NatFloat[] { new NatFloat(0, 0.3125f, EnumDistribution.UNIFORM), new NatFloat(0, 0, EnumDistribution.UNIFORM), new NatFloat(0, 0.3125f, EnumDistribution.UNIFORM) };
			fireParticles.Velocity = new NatFloat[] { new NatFloat(0, 0.025f, EnumDistribution.UNIFORM), new NatFloat(0.18f, 0, EnumDistribution.UNIFORM), new NatFloat(0, 0.025f, EnumDistribution.UNIFORM) };
			fireParticles.Quantity = new NatFloat(1f, 1.5f, EnumDistribution.UNIFORM);
			fireParticles.LifeLength = new NatFloat(0.75f, 0.25f, EnumDistribution.UNIFORM);
			fireParticles.Size = new NatFloat(0.25f, 0.05f, EnumDistribution.UNIFORM);
			fireParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARINCREASE, -0.25f);
			fireParticles.ParticleModel = EnumParticleModel.Quad;
			fireParticles.VertexFlags = 128;
			fireParticles.WindAffectednes = 0.7f;
			fireParticles.WindAffectednesAtPos = 0.3f;
			fireParticles.SelfPropelled = true;
		}
	}
}