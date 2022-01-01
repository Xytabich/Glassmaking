using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks
{
    public class BlockEntityFirebox : BlockEntity, ITimeBasedHeatSource
    {
        private const float TEMP_INCREASE_PER_HOUR = 1500;
        private const float TEMP_DECREASE_PER_HOUR = 2000;

        private static SimpleParticleProperties smokeParticles;
        private static AdvancedParticleProperties fireParticles;

        protected virtual float fuelTemperatureModifier => 1.1f;
        protected virtual float fuelDurationModifier => 0.8f;
        protected virtual int maxFuelCount => 16;

        private ItemStack contents = null;

        private BlockRendererFirebox renderer = null;

        private ITimeBasedHeatReceiver receiver = null;

        private bool burning = false;
        private float temperature = 20;
        /// <summary>
        /// How much burning time is left
        /// </summary>
        private float fuelLevel = 0f;
        // Fuel parameters
        private float fuelTemperature = 0f;
        private float fuelBurnDuration = 0f;

        private double lastTickTime;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if(contents != null)
            {
                contents.ResolveBlockOrItem(Api.World);
                ApplyFuelParameters();
            }
            SetReceiver(api.World.BlockAccessor.GetBlockEntity(Pos.UpCopy()) as ITimeBasedHeatReceiver);
            if(api.Side == EnumAppSide.Client)
            {
                ICoreClientAPI coreClientAPI = (ICoreClientAPI)api;
                renderer = new BlockRendererFirebox(Pos, coreClientAPI);
                coreClientAPI.Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "glassmaking:firebox");
                UpdateRendererFull();
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
                    dsc.AppendLine("Contents: " + contents.StackSize + "x " + contents.GetName());
                }
                else
                {
                    dsc.AppendLine("Fuel: " + contents.GetName());
                }
            }
            if(temperature > 25)
            {
                dsc.AppendLine("Temperature: " + temperature.ToString("#"));
            }
            if(burning)
            {
                var calendar = Api.World.Calendar;
                dsc.AppendLine("Burn time: " + ((fuelLevel + GetFuelCount() * fuelBurnDuration) * 3600 / (calendar.SpeedOfTime * calendar.CalendarSpeedMul)).ToString("0"));
            }
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

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            contents = tree.GetItemstack("fuelStack");
            fuelLevel = tree.GetFloat("fuelLevel");
            temperature = tree.GetFloat("temperature", 20);
            burning = tree.GetBool("burning");
            lastTickTime = tree.GetDouble("lastTickTotalHours");
            if(contents != null && Api?.World != null)
            {
                contents.ResolveBlockOrItem(Api.World);
                ApplyFuelParameters();
            }
            UpdateRendererFull();
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetItemstack("fuelStack", contents);
            tree.SetFloat("fuelLevel", fuelLevel);
            tree.SetFloat("temperature", temperature);
            tree.SetBool("burning", burning);
            tree.SetDouble("lastTickTotalHours", lastTickTime);
        }

        public void SetReceiver(ITimeBasedHeatReceiver receiver)
        {
            if(this.receiver != receiver)
            {
                if(this.receiver != null) this.receiver.SetHeatSource(null);
                this.receiver = receiver;
                if(receiver != null) receiver.SetHeatSource(this);
            }
        }

        public ItemStack[] GetDropItems()
        {
            if(contents == null || contents.StackSize < 1) return null;
            return new ItemStack[] { contents.Clone() };
        }

        public bool TryAdd(IPlayer byPlayer, ItemSlot slot, int count)
        {
            var combustibleProps = slot.Itemstack.Collectible.CombustibleProps;
            if(combustibleProps == null || combustibleProps.BurnTemperature < 100) return false;
            if(burning)
            {
                if(contents == null) return false;
                if(!contents.Satisfies(slot.Itemstack)) return false;
            }
            else
            {
                if(contents != null && !contents.Satisfies(slot.Itemstack)) return false;
            }

            int consume = Math.Min(maxFuelCount - GetFuelCount(), Math.Min(slot.Itemstack.StackSize, count));
            if(consume > 0)
            {
                if(contents == null)
                {
                    contents = slot.Itemstack.GetEmptyClone();
                    contents.StackSize = consume;
                    if(!burning && temperature >= 300)
                    {
                        burning = true;
                        ApplyFuelParameters();
                        fuelLevel = fuelBurnDuration;
                        contents.StackSize--;
                    }
                    UpdateRendererFull();
                }
                else
                {
                    contents.StackSize += consume;
                }
                if(slot.Itemstack.StackSize > consume)
                {
                    slot.Itemstack.StackSize -= consume;
                }
                else
                {
                    slot.Itemstack = null;
                }
                slot.MarkDirty();
                MarkDirty(true);
                return true;
            }
            return false;
        }

        public bool CanIgnite()
        {
            return !burning && GetFuelCount() > 0;
        }

        public void TryIgnite()
        {
            burning = true;
            ApplyFuelParameters();
            fuelLevel = fuelBurnDuration;
            if(contents.StackSize > 0) contents.StackSize--;
            lastTickTime = Api.World.Calendar.TotalHours;
            UpdateRendererFull();
            MarkDirty(true);
        }

        public bool IsBurning()
        {
            return burning;
        }

        public bool IsHeatedUp()
        {
            return temperature > 20;
        }

        public float GetTemperature()
        {
            return temperature;
        }

        public double GetLastTickTime()
        {
            double totalHours = Api.World.Calendar.TotalHours;
            return lastTickTime > totalHours ? totalHours : lastTickTime;
        }

        public double CalcTempElapsedTime(double startTime, float temperature)
        {
            if(fuelTemperature < temperature) return 0;

            double time = 0;
            float temp = this.temperature;
            double hours = Api.World.Calendar.TotalHours - lastTickTime;
            if(burning && hours > 0)
            {
                double burnTime = fuelLevel + fuelBurnDuration * GetFuelCount();
                if(temp < temperature)
                {
                    double t = Math.Min((temperature - temp) / TEMP_INCREASE_PER_HOUR, Math.Min(hours, burnTime));
                    temp += (float)(t * TEMP_INCREASE_PER_HOUR);
                    hours -= t;
                    burnTime -= t;
                    startTime -= t;
                }
                if(hours > 0)
                {
                    time = Math.Min(burnTime, hours);
                    hours -= time;
                }
            }
            if(hours > 0)
            {
                time += Math.Min((temp - temperature) / TEMP_DECREASE_PER_HOUR, hours) * TEMP_DECREASE_PER_HOUR;
            }
            return Math.Max(time - Math.Max(startTime, 0), 0);
        }

        public float CalcCurrentTemperature()
        {
            float temp = temperature;
            double hours = Api.World.Calendar.TotalHours - lastTickTime;
            if(burning && hours > 0)
            {
                double burnTime = fuelLevel + fuelBurnDuration * GetFuelCount();
                if(temp < fuelTemperature)
                {
                    double time = Math.Min((fuelTemperature - temp) / TEMP_INCREASE_PER_HOUR, Math.Min(hours, burnTime));
                    temp -= (float)(time * TEMP_INCREASE_PER_HOUR);
                    hours -= time;
                    burnTime -= time;
                }
                if(temp < fuelTemperature)
                {
                    double time = Math.Min((fuelTemperature - temp) / TEMP_INCREASE_PER_HOUR, Math.Min(hours, burnTime));
                    temp += (float)(time * TEMP_INCREASE_PER_HOUR);
                    hours -= time;
                    burnTime -= time;
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
            return temp;
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
                    if(usedFuelCount > 0)
                    {
                        contents.StackSize -= usedFuelCount;
                        if(contents.StackSize <= 0)
                        {
                            burning = fuelLevel > 0;
                            if(!burning) contents = null;
                        }
                        UpdateRendererFull();
                        MarkDirty(true);
                    }
                }
                else
                {
                    fuelLevel = (float)burnTime;
                    burning = fuelLevel > 0;
                    if(!burning)
                    {
                        contents = null;
                        UpdateRendererFull();
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
            if(contents == null) return 0;
            return contents.StackSize;
        }

        private void ApplyFuelParameters()
        {
            var combustibleProps = contents.Collectible.CombustibleProps;//TODO: smoke level?
            fuelTemperature = combustibleProps.BurnTemperature * fuelTemperatureModifier;
            var calendar = Api.World.Calendar;
            fuelBurnDuration = combustibleProps.BurnDuration * fuelDurationModifier * calendar.SpeedOfTime * calendar.CalendarSpeedMul;
            fuelBurnDuration *= 1f / 3600f;
        }

        private void UpdateRendererFull()
        {
            if(Api != null && Api.Side == EnumAppSide.Client && renderer != null)
            {
                renderer.SetHeight(GetFuelHeight());
                UpdateRendererParameters();
            }
        }

        private int GetFuelHeight()
        {
            return GetFuelCount() + (burning ? 1 : 0);
        }

        private void UpdateRendererParameters()
        {
            renderer.SetParameters(burning, Math.Min(128, (int)((temperature / 1500f) * 128)));
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