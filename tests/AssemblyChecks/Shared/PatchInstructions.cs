// Reads installed IL for transpiler checks without initializing Harmony's native detour runtime.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace MoreSailwindSails.Tests.AssemblyChecks.Shared;

internal static class PatchInstructions
{
    internal static List<CodeInstruction> Read(MethodInfo method, ILGenerator generator)
    {
        var body = method.GetMethodBody();
        var locals = body
            .LocalVariables.Select(l => generator.DeclareLocal(l.LocalType, l.IsPinned))
            .ToArray();
        var opcodes = typeof(OpCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(OpCode))
            .Select(f => (OpCode)f.GetValue(null))
            .ToDictionary(op => unchecked((ushort)op.Value));
        var code = new Dictionary<int, CodeInstruction>();
        using var reader = new BinaryReader(new MemoryStream(body.GetILAsByteArray()));
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            int offset = (int)reader.BaseStream.Position;
            ushort key = reader.ReadByte();
            if (key == 0xfe)
                key = (ushort)(0xfe00 | reader.ReadByte());
            var opcode = opcodes[key];
            object operand = null;
            switch (opcode.OperandType)
            {
                case OperandType.InlineNone:
                    break;
                case OperandType.ShortInlineBrTarget:
                    operand = reader.ReadSByte() + (int)reader.BaseStream.Position;
                    break;
                case OperandType.InlineBrTarget:
                    operand = reader.ReadInt32() + (int)reader.BaseStream.Position;
                    break;
                case OperandType.InlineSwitch:
                    var offsets = Enumerable
                        .Range(0, reader.ReadInt32())
                        .Select(_ => reader.ReadInt32())
                        .ToArray();
                    operand = offsets.Select(o => o + (int)reader.BaseStream.Position).ToArray();
                    break;
                case OperandType.InlineString:
                    operand = method.Module.ResolveString(reader.ReadInt32());
                    break;
                case OperandType.InlineMethod:
                case OperandType.InlineField:
                case OperandType.InlineType:
                case OperandType.InlineTok:
                    operand = method.Module.ResolveMember(
                        reader.ReadInt32(),
                        method.DeclaringType.GetGenericArguments(),
                        method.GetGenericArguments()
                    );
                    break;
                case OperandType.ShortInlineVar:
                case OperandType.InlineVar:
                    int index =
                        opcode.OperandType == OperandType.ShortInlineVar
                            ? reader.ReadByte()
                            : reader.ReadUInt16();
                    operand = opcode.Name.Contains("loc") ? locals[index] : (object)index;
                    break;
                case OperandType.ShortInlineI:
                    operand = reader.ReadSByte();
                    break;
                case OperandType.InlineI:
                    operand = reader.ReadInt32();
                    break;
                case OperandType.InlineI8:
                    operand = reader.ReadInt64();
                    break;
                case OperandType.ShortInlineR:
                    operand = reader.ReadSingle();
                    break;
                case OperandType.InlineR:
                    operand = reader.ReadDouble();
                    break;
                default:
                    throw new Exception("Unsupported test IL operand: " + opcode);
            }
            code.Add(offset, new CodeInstruction(opcode, operand));
        }
        var labels = new Dictionary<int, Label>();
        Label Target(int offset)
        {
            if (!labels.TryGetValue(offset, out var label))
            {
                labels[offset] = label = generator.DefineLabel();
                code[offset].labels.Add(label);
            }
            return label;
        }
        foreach (var instruction in code.Values)
        {
            if (
                instruction.opcode.OperandType
                is OperandType.ShortInlineBrTarget
                    or OperandType.InlineBrTarget
            )
                instruction.operand = Target((int)instruction.operand);
            else if (instruction.opcode.OperandType == OperandType.InlineSwitch)
                instruction.operand = ((int[])instruction.operand).Select(Target).ToArray();
        }
        foreach (var clause in body.ExceptionHandlingClauses)
        {
            if (clause.Flags != ExceptionHandlingClauseOptions.Finally)
                throw new Exception("Unexpected installed exception handler; extend test decoder.");
            code[clause.TryOffset]
                .blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
            code[clause.HandlerOffset]
                .blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));
            code.Last(p => p.Key < clause.HandlerOffset + clause.HandlerLength)
                .Value.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
        }
        return code.Values.ToList();
    }
}
