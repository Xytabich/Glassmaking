using GlassMaking.Common;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace GlassMaking.Blocks
{
	public class BlockEntityGlassCastingMold : BlockEntity, IGlassmeltSink
	{
		public int fillLevel = 0;

		public float Temperature { get { return temperature.GetTemperature(Api.World); } }

		public bool IsHardened { get { return Temperature < 0.45f * mod.GetGlassTypeInfo(glassCode).meltingPoint; } }

		public bool IsLiquid { get { return Temperature > mod.GetGlassTypeInfo(glassCode).meltingPoint; } }

		public bool IsFull { get { return fillLevel >= requiredUnits; } }

		public bool IsEmpty { get { return glassCode == null; } }

		private Cuboidf[] fillQuadsByLevel = null;
		private int requiredUnits = 100;
		private float fillHeight = 1;

		private AssetLocation glassCode = null;
		private TemperatureContainer temperature = new TemperatureContainer();

		private CastingMoldRenderer renderer = null;
		private GlassMakingMod mod;

		public override void Initialize(ICoreAPI api)
		{
			base.Initialize(api);

			if(Block == null || Block.Code == null || Block.Attributes == null) return;

			mod = api.ModLoader.GetModSystem<GlassMakingMod>();

			var block = (BlockGlassCastingMold)Block;
			if(block.Recipes == null || block.Recipes.Length == 0) return;

			requiredUnits = block.Recipes[0].Recipe.Amount;

			if(api is ICoreClientAPI)
			{
				ICoreClientAPI capi = (ICoreClientAPI)api;

				if(Block.Attributes["fillQuadsByLevel"].Exists)
				{
					fillQuadsByLevel = Array.ConvertAll(Block.Attributes["fillQuadsByLevel"].AsObject<RotatableCube[]>(), c => c.RotatedCopy());
				}

				if(fillQuadsByLevel == null)
				{
					fillQuadsByLevel = new Cuboidf[] { new Cuboidf(2, 0, 2, 14, 0, 14), };
				}

				fillHeight = Block.Attributes["fillHeight"].AsFloat(1);

				renderer = new CastingMoldRenderer(Pos, capi, fillQuadsByLevel);

				UpdateRenderer();

				RegisterGameTickListener(OnGameTick, 50);
			}
		}

		public bool CanReceiveGlass(AssetLocation code, int amount)
		{
			if(glassCode == null)
			{
				if(FindRecipe(code) != null) return true;
			}
			return code.Equals(glassCode) && fillLevel < requiredUnits && IsLiquid;
		}

		public void ReceiveGlass(EntityAgent byEntity, AssetLocation code, ref int amount, float temperature)
		{
			if(glassCode == null)
			{
				glassCode = code;
				fillLevel = 0;
			}
			this.temperature.SetTemperature(Api.World, temperature);
			int amountToFill;
			if(Api.Side == EnumAppSide.Client)
			{
				// Prevent the last unit from filling up until the server has finished
				amountToFill = Math.Min(amount, (requiredUnits - 1) - fillLevel);
			}
			else
			{
				amountToFill = Math.Min(amount, requiredUnits - fillLevel);
			}
			fillLevel += amountToFill;
			amount -= amountToFill;
			UpdateRenderer();
		}

		public void OnPourOver()
		{
			MarkDirty(true);
		}

		public bool OnPlayerInteract(IPlayer byPlayer, BlockFacing onFace, Vec3d hitPosition)
		{
			bool sneaking = byPlayer.Entity.Controls.Sneak;

			if(!sneaking)
			{
				if(byPlayer.Entity.Controls.HandUse != EnumHandInteract.None) return false;

				bool handled = TryTakeContents(byPlayer);

				if(!handled && fillLevel == 0)
				{
					ItemSlot activeSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
					if(activeSlot.Itemstack == null || activeSlot.Itemstack.Collectible is BlockGlassCastingMold)
					{
						if(!byPlayer.InventoryManager.TryGiveItemstack(new ItemStack(Block)))
						{
							Api.World.SpawnItemEntity(new ItemStack(Block), Pos.ToVec3d().Add(0.5, 0.2, 0.5));
						}

						Api.World.BlockAccessor.SetBlock(0, Pos);

						if(Block.Sounds?.Place != null)
						{
							Api.World.PlaySoundAt(Block.Sounds.Place, Pos.X, Pos.Y, Pos.Z, byPlayer, false);
						}

						handled = true;
					}
				}

				return handled;
			}

			return false;
		}

		public override void OnBlockRemoved()
		{
			base.OnBlockRemoved();

			renderer?.Dispose();
			renderer = null;
		}

		public override void OnBlockUnloaded()
		{
			base.OnBlockUnloaded();
			renderer?.Dispose();
			renderer = null;
		}

		public IEnumerable<ItemStack> GetDrops()
		{
			if(glassCode == null) return null;
			if(fillLevel < requiredUnits || !IsHardened)
			{
				return GlassBlend.GetShardsList(Api.World, glassCode, fillLevel);
			}
			return GetReadyMoldedStacks();
		}

		public ItemStack[] GetReadyMoldedStacks()
		{
			if(glassCode == null) return null;
			if(fillLevel < requiredUnits || !IsHardened) return null;

			var recipe = FindRecipe(glassCode);
			if(recipe == null) return null;

			return new ItemStack[] { recipe.Output.ResolvedItemstack.Clone() };
		}

		public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolve)
		{
			base.FromTreeAttributes(tree, worldForResolve);

			var code = tree.GetString("glassCode");
			glassCode = string.IsNullOrEmpty(code) ? null : new AssetLocation(code);
			fillLevel = tree.GetInt("fillLevel");
			temperature.FromTreeAttributes(tree, "temperature");

			UpdateRenderer();

			if(Api?.Side == EnumAppSide.Client)
			{
				Api.World.BlockAccessor.MarkBlockDirty(Pos);
			}
		}

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
			base.ToTreeAttributes(tree);

			if(glassCode != null) tree.SetString("glassCode", glassCode.ToString());
			tree.SetInt("fillLevel", fillLevel);
			temperature.ToTreeAttributes(tree, "temperature");
		}

		public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
		{
			string contents = "";
			if(glassCode != null)
			{
				string state = IsLiquid ? Lang.Get("liquid") : (IsHardened ? Lang.Get("hardened") : Lang.Get("soft"));

				string temp = Temperature < 21 ? Lang.Get("Cold") : Lang.Get("{0}°C", (int)Temperature);
				contents = Lang.Get("glassmaking:{0}/{4} units of {1} {2} glass ({3})", fillLevel, state, Lang.Get(GlassBlend.GetBlendNameCode(glassCode)), temp, requiredUnits);
			}
			dsc.AppendLine(contents.Length == 0 ? Lang.Get("Empty") : contents);
		}

		protected virtual bool TryTakeContents(IPlayer byPlayer)
		{
			if(Api is ICoreServerAPI) MarkDirty();

			if(glassCode != null && fillLevel >= requiredUnits && IsHardened)
			{
				Api.World.PlaySoundAt(new AssetLocation("sounds/block/ingot"), Pos.X, Pos.Y, Pos.Z, byPlayer, false);

				if(Api is ICoreServerAPI)
				{
					ItemStack[] outstacks = GetReadyMoldedStacks();

					if(outstacks != null)
					{
						foreach(ItemStack outstack in outstacks)
						{
							outstack.Collectible.SetTemperature(Api.World, outstack, Temperature);

							if(!byPlayer.InventoryManager.TryGiveItemstack(outstack))
							{
								Api.World.SpawnItemEntity(outstack, Pos.ToVec3d().Add(0.5, 0.2, 0.5));
							}
						}
					}

					glassCode = null;
					fillLevel = 0;
				}

				UpdateRenderer();

				return true;
			}

			return false;
		}

		private CastingMoldRecipe FindRecipe(AssetLocation glassCode)
		{
			var recipes = ((BlockGlassCastingMold)Block).GetRecipes();
			if(recipes == null || recipes.Length == 0) return null;
			for(int i = 0; i < recipes.Length; i++)
			{
				if(recipes[i].Recipe?.Code.Equals(glassCode) == true)
				{
					return recipes[i];
				}
			}
			return null;
		}

		private void OnGameTick(float dt)
		{
			if(renderer != null)
			{
				renderer.Level = (float)fillLevel * fillHeight / requiredUnits;
			}

			if(glassCode != null && renderer != null)
			{
				renderer.Temperature = Math.Min(1300, Temperature);
			}
		}

		private void UpdateRenderer()
		{
			if(renderer == null) return;

			renderer.Level = fillLevel * fillHeight / requiredUnits;

			if(glassCode != null)
			{
				renderer.TextureName = glassCode.Clone().WithPathPrefix("block/glass/").WithPathAppendix(".png");
			}
			else
			{
				renderer.TextureName = null;
			}
		}

		private class TemperatureContainer
		{
			public float temperature;
			public double lastUpdate;

			public float GetTemperature(IWorldAccessor world)
			{
				double totalHours = world.Calendar.TotalHours;
				double lastUpdate = this.lastUpdate;
				if(totalHours - lastUpdate > 1.0 / 85)
				{
					float temperature = Math.Max(20f, this.temperature - Math.Max(0f, (float)(totalHours - lastUpdate) * 180f));
					SetTemperature(world, temperature);
					return temperature;
				}
				return this.temperature;
			}

			public void SetTemperature(IWorldAccessor world, float temperature, bool delayCooldown = false)
			{
				double totalHours = world.Calendar.TotalHours;
				float prevTemperature = this.temperature;
				if(delayCooldown && prevTemperature < temperature)
				{
					totalHours += 0.5;
				}

				this.lastUpdate = totalHours;
				this.temperature = temperature;
			}

			public void FromTreeAttributes(ITreeAttribute container, string key)
			{
				var attr = container[key] as ITreeAttribute;
				if(attr == null)
				{
					temperature = 20f;
					return;
				}
				temperature = attr.GetFloat("temperature", 20);
				lastUpdate = attr.GetDouble("temperatureLastUpdate");
			}

			public void ToTreeAttributes(ITreeAttribute container, string key)
			{
				var attr = container.GetOrAddTreeAttribute(key);
				attr.SetFloat("temperature", temperature);
				attr.SetDouble("temperatureLastUpdate", lastUpdate);
			}
		}
	}
}