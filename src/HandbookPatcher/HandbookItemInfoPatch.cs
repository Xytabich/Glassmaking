using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace GlassMaking
{
	[HarmonyPatch(typeof(CollectibleBehaviorHandbookTextAndExtraInfo), "GetHandbookInfo")]
	internal static class HandbookItemInfoPatch
	{
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
		{
			var insts = new List<CodeInstruction>(instructions);
			var body = original.GetMethodBody();
			var locals = body.LocalVariables;

			int listLocIndex = -1;

			int index = 0;
			while(index < insts.Count)
			{
				if(TryGetStloc(insts[index], out listLocIndex))
				{
					if(typeof(List<RichTextComponentBase>).IsAssignableFrom(locals[listLocIndex].LocalType))
					{
						index++;
						break;
					}
				}
				index++;
			}
			if(index >= insts.Count) throw new InvalidOperationException();

			InsertEventCall(insts, ref index, listLocIndex, HandbookItemInfoSection.BeforeAll);

			while(index < insts.Count)
			{
				if(insts[index].opcode == OpCodes.Newobj)
				{
					if(typeof(ClearFloatTextComponent).IsAssignableFrom(((ConstructorInfo)insts[index].operand).DeclaringType))
					{
						index++;
						InsertEventCall(insts, ref index, listLocIndex, HandbookItemInfoSection.AfterItemHeader);
						break;
					}
				}
				index++;
			}
			if(index >= insts.Count) throw new InvalidOperationException();

			FieldInfo extraSectionsField = typeof(CollectibleBehaviorHandbookTextAndExtraInfo).GetField(nameof(CollectibleBehaviorHandbookTextAndExtraInfo.ExtraHandBookSections));

			while(index < insts.Count)
			{
				if(insts[index].opcode == OpCodes.Ldfld)
				{
					if(extraSectionsField == (FieldInfo)insts[index].operand)
					{
						index++;
						InsertEventCall(insts, ref index, listLocIndex, HandbookItemInfoSection.BeforeExtraSections);
						break;
					}
				}
				index++;
			}
			if(index >= insts.Count) throw new InvalidOperationException();

			while(index < insts.Count)
			{
				if(insts[index].opcode == OpCodes.Ldstr)
				{
					if(((string)insts[index].operand).Contains("handbooktitle"))
					{
						index++;
						InsertEventCall(insts, ref index, listLocIndex, HandbookItemInfoSection.BeforeHandbookInfo);
						break;
					}
				}
				index++;
			}
			if(index >= insts.Count) throw new InvalidOperationException();

			while(index < insts.Count)
			{
				if(insts[index].opcode == OpCodes.Ret)
				{
					index--;
					InsertEventCall(insts, ref index, listLocIndex, HandbookItemInfoSection.AfterAll);
					index++;
					index++;
					break;
				}
				index++;
			}
			for(int i = 0; i < insts.Count; i++)
			{
				yield return insts[i];
			}
		}

		private static bool TryGetStloc(CodeInstruction inst, out int index)
		{
			switch(inst.opcode.Value)
			{
				case 0x0A:
					index = 0;
					return true;
				case 0x0B:
					index = 1;
					return true;
				case 0x0C:
					index = 2;
					return true;
				case 0x0D:
					index = 3;
					return true;
				case 0x13:
					index = ((LocalBuilder)inst.operand).LocalIndex;
					return true;
			}
			if(inst.opcode == OpCodes.Stloc)
			{
				index = ((LocalBuilder)inst.operand).LocalIndex;
				return true;
			}
			index = default;
			return false;
		}

		private static void InsertEventCall(List<CodeInstruction> insts, ref int index, int listLocIndex, HandbookItemInfoSection section)
		{
			insts.Insert(index, new CodeInstruction(OpCodes.Ldarg_1));
			index++;
			insts.Insert(index, new CodeInstruction(OpCodes.Ldarg_2));
			index++;
			insts.Insert(index, new CodeInstruction(OpCodes.Ldarg_3));
			index++;
			insts.Insert(index, new CodeInstruction(OpCodes.Ldarg_S, 4));
			index++;
			insts.Insert(index, new CodeInstruction(OpCodes.Ldc_I4_S, (int)section));
			index++;
			insts.Insert(index, new CodeInstruction(OpCodes.Ldloc, listLocIndex));
			index++;
			insts.Insert(index, CodeInstruction.Call(() => HandbookItemInfoEvent.GetHandbookInfo(default, default, default, default, default, default)));
			index++;
		}
	}
}