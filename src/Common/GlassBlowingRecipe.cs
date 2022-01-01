using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking
{
    public class GlassBlowingRecipe : IByteSerializable, IRecipeBase<GlassBlowingRecipe>
    {
        public int recipeId;

        public AssetLocation code;

        public JsonItemStack output;

        public JsonGlassBlowingToolStep[] steps;

        public GlassBlowingToolStep[] resolvedSteps;

        public AssetLocation Name { get; set; }

        public bool Enabled { get; set; } = true;

        public IRecipeIngredient[] Ingredients { get; } = new IRecipeIngredient[0];

        public IRecipeOutput Output => output;

        public bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
        {
            if(steps == null || steps.Length == 0 || output == null)
            {
                world.Logger.Error("Glassblowing recipe with output {0} has no steps or missing output. Ignoring recipe.", Output);
                return false;
            }
            if(!output.Resolve(world, sourceForErrorLogging))
            {
                return false;
            }
            var system = world.Api.ModLoader.GetModSystem<GlassMakingMod>();
            resolvedSteps = new GlassBlowingToolStep[steps.Length];
            for(int i = 0; i < steps.Length; i++)
            {
                var tool = system.GetGlassBlowingTool(steps[i].tool);
                if(tool == null)
                {
                    world.Logger.Error("Failed resolving a glassblowing tool with code {0} in {1}", steps[i].tool, sourceForErrorLogging);
                    return false;
                }
                var step = tool.GetStepInstance();
                step.tool = steps[i].tool.Clone();
                step.shape = steps[i].GenRadii();
                if(!step.Resolve(steps[i].attributes, world, sourceForErrorLogging))
                {
                    return false;
                }
                resolvedSteps[i] = step;
            }
            return true;
        }

        public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
        {
            return new Dictionary<string, string[]>();
        }

        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(code);
            writer.Write(resolvedSteps.Length);
            for(int i = 0; i < resolvedSteps.Length; i++)
            {
                writer.Write(resolvedSteps[i].tool);
                resolvedSteps[i].ToBytes(writer);
            }
            output.ToBytes(writer);
        }

        public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            code = reader.ReadAssetLocation();
            resolvedSteps = new GlassBlowingToolStep[reader.ReadInt32()];
            var system = resolver.Api.ModLoader.GetModSystem<GlassMakingMod>();
            for(int i = 0; i < resolvedSteps.Length; i++)
            {
                var code = reader.ReadAssetLocation();
                var tool = system.GetGlassBlowingTool(code);
                resolvedSteps[i] = tool.GetStepInstance();
                resolvedSteps[i].tool = code;
                resolvedSteps[i].FromBytes(reader, resolver);
            }
            output = new JsonItemStack();
            output.FromBytes(reader, resolver.ClassRegistry);
            output.Resolve(resolver, "[FromBytes]");
        }

        public GlassBlowingRecipe Clone()
        {
            return new GlassBlowingRecipe() {
                recipeId = recipeId,
                code = code.Clone(),
                output = output.Clone(),
                resolvedSteps = Array.ConvertAll(resolvedSteps, CloneStep),
                Name = Name.Clone(),
                Enabled = Enabled
            };
        }

        private static GlassBlowingToolStep CloneStep(GlassBlowingToolStep other)
        {
            return other.Clone();
        }
    }

    public sealed class JsonGlassBlowingToolStep
    {
        public AssetLocation tool;

        public string[] shape;

        [JsonProperty]
        [JsonConverter(typeof(JsonAttributesConverter))]
        public JsonObject attributes;

        public int[,] GenRadii()
        {
            if(shape != null && shape.Length > 0)
            {
                var shapeRadii = new int[shape.Length, 2];
                for(int i = 0; i < shape.Length; i++)
                {
                    string str = shape[i];
                    int innerRadius = 0;
                    int index = 0;
                    for(; index < str.Length; index++)
                    {
                        if(str[index] == '_') innerRadius++;
                        if(str[index] == '#') break;
                    }
                    innerRadius = GameMath.Clamp(innerRadius, 0, 15);
                    int outerRadius = 0;
                    for(; index < str.Length; index++)
                    {
                        if(str[index] != '#') break;
                        outerRadius++;
                    }
                    outerRadius = GameMath.Clamp(innerRadius + outerRadius, innerRadius + 1, 16);
                    shapeRadii[i, 0] = innerRadius;
                    shapeRadii[i, 1] = outerRadius;
                }
                return shapeRadii;
            }
            else
            {
                return null;
            }
        }
    }

    public abstract class GlassBlowingToolStep
    {
        public AssetLocation tool;

        public int[,] shape;

        public bool hasShape => shape != null && shape.GetLength(0) > 0;

        public abstract bool Resolve(JsonObject attributes, IWorldAccessor world, string sourceForErrorLogging);

        public virtual void ToBytes(BinaryWriter writer)
        {
            if(shape == null) writer.Write(0);
            else
            {
                int length = shape.GetLength(0);
                writer.Write(length);
                for(int i = 0; i < length; i++)
                {
                    writer.Write((byte)((shape[i, 0] & 15) | ((shape[i, 1] << 4) & 15)));
                }
            }
        }

        public virtual void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            int length = reader.ReadInt32();
            shape = new int[length, 2];
            for(int i = 0; i < length; i++)
            {
                var data = reader.ReadByte();
                shape[i, 0] = data & 15;
                shape[i, 1] = (data >> 4) & 15;
            }
        }

        public abstract GlassBlowingToolStep Clone();
    }
}