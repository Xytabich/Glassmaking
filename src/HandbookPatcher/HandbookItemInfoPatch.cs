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
			var locals = body!.LocalVariables;

			const BindingFlags flags = BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic;
			var addGeneralInfoMethod = typeof(CollectibleBehaviorHandbookTextAndExtraInfo).GetMethod("addGeneralInfo", flags);
			var addExtraSectionsMethod = typeof(CollectibleBehaviorHandbookTextAndExtraInfo).GetMethod("addExtraSections", flags);

			int listLocIndex = -1;
			for(int i = 0; i < locals.Count; i++)
			{
				if(locals[i].LocalType == typeof(List<RichTextComponentBase>))
				{
					listLocIndex = i;
					break;
				}
			}
			if(listLocIndex < 0) throw new InvalidOperationException();

			int index = 0;
			while(index < insts.Count)
			{
				if(insts[index].Calls(addGeneralInfoMethod))
				{
					InsertEventCall(insts, ref index, listLocIndex, HandbookItemInfoSection.BeforeAll);
					index++;
					InsertEventCall(insts, ref index, listLocIndex, HandbookItemInfoSection.AfterItemHeader);
					break;
				}
				index++;
			}
			if(index >= insts.Count) throw new InvalidOperationException();

			while(index < insts.Count)
			{
				if(insts[index].Calls(addExtraSectionsMethod))
				{
					InsertEventCall(insts, ref index, listLocIndex, HandbookItemInfoSection.BeforeExtraSections);
					index++;
					InsertEventCall(insts, ref index, listLocIndex, HandbookItemInfoSection.BeforeStorableInfo);
					break;
				}
				index++;
			}
			if(index >= insts.Count) throw new InvalidOperationException();

			while(index < insts.Count)
			{
				if(insts[index].opcode == OpCodes.Ret)
				{
					InsertEventCall(insts, ref index, listLocIndex, HandbookItemInfoSection.AfterAll);
					break;
				}
				index++;
			}
			return insts;
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
			insts.Insert(index, CodeInstruction.Call(() => HandbookItemInfoEvent.GetHandbookInfo(default!, default!, default!, default!, default, default!)));
			index++;
		}
	}
}