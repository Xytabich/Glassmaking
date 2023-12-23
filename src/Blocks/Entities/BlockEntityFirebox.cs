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
	public class BlockEntityFirebox : BlockEntity, ITimeBasedHeatSource, ITimeBasedHeatSourceContainer, ITimeBasedHeatSourceControl, IHeatSource
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
		private IHeatSourceModifier modifier = null;

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
		private float durationModifier => modifier == null ? 1f : modifier.FuelRateModifier;

		private double lastTickTime;
		private bool initTickTime = true;

		public override void Initialize(ICoreAPI api)
		{
			base.Initialize(api);
			inventory.LateInitialize("firebox-1", api);
			if(contents != null) ApplyFuelParameters();
			SetReceiver(api.World.BlockAccessor.GetBlockEntity(Pos.UpCopy()) as ITimeBasedHeatReceiver);
			if(api.Side == EnumAppSide.Client)
			{
				ICoreClientAPI capi = (ICoreClientAPI)api;
				renderer = new BlockRendererFirebox(Pos, capi.Tesselator.GetTextureSource(Block), capi);
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
			receiver?.SetHeatSource(null);
			base.OnBlockRemoved();
		}

		public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
		{
			base.FromTreeAttributes(tree, worldAccessForResolve);
			inventory.FromTreeAttributes(tree);
			fuelLevel = tree.GetFloat("fuelLevel");
			temperature = tree.GetFloat("temperature", 20);
			burning = tree.GetBool("burning");
			if(initTickTime)
			{
				initTickTime = false;
				lastTickTime = tree.GetDouble("lastTickTotalHours");
			}
			if(contents != null && Api?.World != null) ApplyFuelParameters();
			UpdateRendererFull();
			ToggleAmbientSounds(burning);
			if(Api?.World != null && Api.Side == EnumAppSide.Client)
			{
				SetReceiver(Api.World.BlockAccessor.GetBlockEntity(Pos.UpCopy()) as ITimeBasedHeatReceiver);
			}
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
				if(this.receiver != null)
				{
					this.receiver.SetHeatSource(null);
				}

				this.receiver = receiver;
				modifier = receiver as IHeatSourceModifier;
				if(receiver != null) receiver.SetHeatSource(this);

				MarkDirty(true);
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

		public ValueGraph CalcHeatGraph(double totalHours = 0)
		{
			float temp = temperature;
			float startTemperature = temp;
			float workingTemperature = temp;

			if(totalHours <= 0) totalHours = Api.World.Calendar.TotalHours;
			double hours = totalHours - lastTickTime;

			double endTime = Math.Max(hours, 0);
			double transitionTime = 0;
			double holdTime = 0;
			double coolingTime = 0;
			int pointCount = 1;

			if(burning && hours > 0)
			{
				double burnTime = fuelLevel + fuelBurnDuration * GetFuelCount();
				if(temp < fuelTemperature)
				{
					double time = Math.Min((fuelTemperature - temp) / TEMP_INCREASE_PER_HOUR, Math.Min(hours, burnTime));
					temp += (float)(time * TEMP_INCREASE_PER_HOUR);
					hours -= time;
					burnTime -= time;
					transitionTime = time;
					workingTemperature = temp;
					if(transitionTime > 0)
					{
						pointCount++;
						endTime -= transitionTime;
					}
				}
				else if(temp > fuelTemperature)
				{
					transitionTime = Math.Min((temp - fuelTemperature) / TEMP_DECREASE_PER_HOUR, hours);
					temp -= (float)(transitionTime * TEMP_DECREASE_PER_HOUR);
					workingTemperature = temp;
					if(transitionTime > 0)
					{
						pointCount++;
						endTime -= transitionTime;
					}
				}
				if(hours > 0)
				{
					holdTime = Math.Min(burnTime, hours);
					hours -= holdTime;
					if(holdTime > 0)
					{
						pointCount++;
						endTime -= holdTime;
					}
				}
			}
			if(hours > 0)
			{
				coolingTime = Math.Min((temp - 20) / TEMP_DECREASE_PER_HOUR, hours);
				temp -= (float)(coolingTime * TEMP_DECREASE_PER_HOUR);
				if(coolingTime > 0)
				{
					pointCount++;
					endTime -= coolingTime;
				}
			}
			if(endTime > 0) pointCount++;

			var graphPoints = new ValueGraph.Point[pointCount];
			int pointIndex = 0;
			double t = 0;
			graphPoints[pointIndex] = new ValueGraph.Point(t, startTemperature);
			if(transitionTime > 0)
			{
				pointIndex++;
				t += transitionTime;
				graphPoints[pointIndex] = new ValueGraph.Point(t, workingTemperature);
			}
			if(holdTime > 0)
			{
				pointIndex++;
				t += holdTime;
				graphPoints[pointIndex] = new ValueGraph.Point(t, workingTemperature);
			}
			if(coolingTime > 0)
			{
				pointIndex++;
				t += coolingTime;
				graphPoints[pointIndex] = new ValueGraph.Point(t, temp);
			}
			if(endTime > 0)
			{
				pointIndex++;
				t += endTime;
				graphPoints[pointIndex] = new ValueGraph.Point(t, temp);
			}

			var graph = new ValueGraph(graphPoints);
			graph.MultiplyValue(temperatureModifier);
			return graph;
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

		public override void OnLoadCollectibleMappings(IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
		{
			base.OnLoadCollectibleMappings(worldForNewMappings, oldBlockIdMapping, oldItemIdMapping, schematicSeed, resolveImports);
			Utils.FixIdMappingOrClear(contentsSlot, oldBlockIdMapping, oldItemIdMapping, worldForNewMappings, resolveImports);
			if(contents == null) burning = false;
		}

		public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
		{
			base.OnStoreCollectibleMappings(blockIdMapping, itemIdMapping);
			contents?.Collectible.OnStoreCollectibleMappings(Api.World, contentsSlot, blockIdMapping, itemIdMapping);
		}

		void ITimeBasedHeatSourceControl.OnTick(float dt)
		{
			OnUpdate();
		}

		private void OnCommonTick(float dt)
		{
			if(receiver == null) OnUpdate();
		}

		private void OnUpdate()
		{
			double totalHours = Api.World.Calendar.TotalHours;
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
			if(Api.World.GetLockFreeBlockAccessor().GetBlockId(new BlockPos(Pos.X, Pos.Y + 1, Pos.Z, 0)) == 0)
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
						Volume = 0.5f,
						Range = 8
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