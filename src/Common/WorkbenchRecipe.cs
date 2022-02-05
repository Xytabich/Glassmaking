using GlassMaking.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking
{
    [JsonObject(MemberSerialization.OptIn)]
    public class WorkbenchRecipe : IRecipeBase, IByteSerializable, IRecipeBase<WorkbenchRecipe>
    {
        public int recipeId;

        [JsonProperty]
        public AssetLocation code;

        [JsonProperty]
        public JsonItemStack input;

        [JsonProperty]
        public JsonItemStack output;

        [JsonProperty]
        public WorkbenchRecipeStep[] steps;

        public AssetLocation Name { get; set; }

        public bool Enabled { get; set; } = true;

        public IRecipeIngredient[] Ingredients { get; } = new IRecipeIngredient[0];

        public IRecipeOutput Output => output;

        AssetLocation IRecipeBase.code => code;

        public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
        {
            return new Dictionary<string, string[]>();
        }

        public bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
        {
            if(steps == null || steps.Length == 0 || input == null || output == null)
            {
                world.Logger.Error("Workbench recipe {0} has no steps or missing output. Ignoring recipe.", code);
                return false;
            }
            foreach(var step in steps)
            {
                step.tool = step.tool.ToLowerInvariant();
            }
            if(!input.Resolve(world, sourceForErrorLogging))
            {
                return false;
            }
            if(!output.Resolve(world, sourceForErrorLogging))
            {
                return false;
            }
            return true;
        }

        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(code);
            writer.Write(steps.Length);
            for(int i = 0; i < steps.Length; i++)
            {
                steps[i].ToBytes(writer);
            }
            input.ToBytes(writer);
            output.ToBytes(writer);
        }

        public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            code = reader.ReadAssetLocation();
            steps = new WorkbenchRecipeStep[reader.ReadInt32()];
            for(int i = 0; i < steps.Length; i++)
            {
                steps[i] = new WorkbenchRecipeStep();
                steps[i].FromBytes(reader);
            }
            input = new JsonItemStack();
            input.FromBytes(reader, resolver.ClassRegistry);
            input.Resolve(resolver, "[FromBytes]");
            output = new JsonItemStack();
            output.FromBytes(reader, resolver.ClassRegistry);
            output.Resolve(resolver, "[FromBytes]");
        }

        public WorkbenchRecipe Clone()
        {
            return new WorkbenchRecipe() {
                recipeId = recipeId,
                code = code.Clone(),
                input = input.Clone(),
                output = output.Clone(),
                steps = Array.ConvertAll(steps, CloneStep),
                Name = Name.Clone(),
                Enabled = Enabled
            };
        }

        private static WorkbenchRecipeStep CloneStep(WorkbenchRecipeStep other)
        {
            return other.Clone();
        }
    }

    [JsonObject]
    public sealed class WorkbenchRecipeStep
    {
        [JsonProperty(Required = Required.Always)]
        public string tool;

        [JsonProperty]
        public CompositeShape shape;

        [JsonProperty]
        [JsonConverter(typeof(JsonAttributesConverter))]
        public JsonObject attributes;

        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(tool);
            writer.Write(shape != null);
            if(shape != null)
            {
                writer.Write(shape.Base);
                writer.Write(shape.InsertBakedTextures);
                writer.Write((short)(shape.rotateX % 360f * 64f));
                writer.Write((short)(shape.rotateY % 360f * 64f));
                writer.Write((short)(shape.rotateZ % 360f * 64f));
                writer.Write(shape.offsetX);
                writer.Write(shape.offsetY);
                writer.Write(shape.offsetZ);
                writer.Write((short)(shape.Scale * 64f));
                writer.Write((byte)shape.Format);
                writer.Write(shape.VoxelizeTexture);
                writer.Write(shape.QuantityElements ?? 0);
            }
            writer.Write(attributes != null);
            if(attributes != null) writer.Write(attributes.Token.ToString());
        }

        public void FromBytes(BinaryReader reader)
        {
            tool = reader.ReadString().ToLowerInvariant();
            if(reader.ReadBoolean())
            {
                shape = new CompositeShape();
                shape.Base = reader.ReadAssetLocation();
                shape.InsertBakedTextures = reader.ReadBoolean();
                shape.rotateX = reader.ReadInt16() / 64f;
                shape.rotateY = reader.ReadInt16() / 64f;
                shape.rotateZ = reader.ReadInt16() / 64f;
                shape.offsetX = reader.ReadSingle();
                shape.offsetY = reader.ReadSingle();
                shape.offsetZ = reader.ReadSingle();
                shape.Scale = reader.ReadInt16() / 64f + 1f;
                shape.Format = (EnumShapeFormat)reader.ReadByte();
                shape.VoxelizeTexture = reader.ReadBoolean();
                shape.QuantityElements = reader.ReadInt32();
            }
            if(reader.ReadBoolean())
            {
                attributes = new JsonObject(JToken.Parse(reader.ReadString()));
            }
        }

        public WorkbenchRecipeStep Clone()
        {
            return new WorkbenchRecipeStep() {
                tool = tool,
                shape = shape.Clone(),
                attributes = attributes?.Clone()
            };
        }
    }
}