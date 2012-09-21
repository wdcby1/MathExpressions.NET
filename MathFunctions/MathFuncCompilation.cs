﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MathFunctions
{
	public class CountNumber
	{
		public int Number;
		public int Count;
		public bool Calculated;

		public override string ToString()
		{
			return string.Format("Number = {0}; Count = {1}; Calculated = {2}", Number, Count, Calculated);
		}
	}

	public struct OpCodeArg
	{
		public OpCode OpCode;
		public object Arg;

		public OpCodeArg(OpCode opCode)
		{
			OpCode = opCode;
			Arg = null;
		}

		public OpCodeArg(OpCode opCode, object arg)
		{
			OpCode = opCode;
			Arg = arg;
		}

		public override string ToString()
		{
			return string.Format("{0,-10}{1}", OpCode, Arg);
		}
	}

	public partial class MathFunc
	{
		const string NamespaceName = "MathFunc";
		const string ClassName = "MathFunc";
		const string FuncName = "Func";

		private struct VariableLifetimeCycle
		{
			public int BeginInd;
			public int EndInd;

			public VariableLifetimeCycle(int beginInd, int endInd)
			{
				BeginInd = beginInd;
				EndInd = endInd;
			}
		}

		private List<OpCodeArg> IlInstructions;
		private Dictionary<KnownMathFunctionType, MethodReference> TypesReferences;
		private int LocalVarNumber;
		private Dictionary<FuncNode, CountNumber> FuncNodes;

		public ICollection<Instruction> Instructions
		{
			get;
			private set;
		}

		public void Compile()
		{
			var name = new AssemblyNameDefinition(NamespaceName, new Version(1, 0, 0, 0));
			var assembly = AssemblyDefinition.CreateAssembly(name, NamespaceName + ".dll", ModuleKind.Dll);

			ImportMath(assembly);

			var globalClass = new TypeDefinition(ClassName, ClassName, 
				TypeAttributes.Public | TypeAttributes.BeforeFieldInit | TypeAttributes.Sealed | TypeAttributes.AnsiClass);

			var globalBody = new MethodDefinition(FuncName,
				MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, assembly.MainModule.TypeSystem.Void);
			
			var globalIlProcessor = globalBody.Body.GetILProcessor();

			DefineLocals();
			IlInstructions = new List<OpCodeArg>();
			EmitNode(Root);
			IlInstructions.Add(new OpCodeArg(OpCodes.Ret));
			OptimizeInstructions();
			OptimizeLocalVariables(ref LocalVarNumber);

			var doubleType = assembly.MainModule.Import(typeof(double));
			for (int i = 0; i < LocalVarNumber; i++)
				globalBody.Body.Variables.Add(new VariableDefinition(doubleType));

			foreach (var instr in IlInstructions)
				EmitInstruction(globalIlProcessor, instr);

			Instructions = globalBody.Body.Instructions;

			globalClass.Methods.Add(globalBody);
			assembly.MainModule.Types.Add(globalClass);

			assembly.Write(NamespaceName + ".dll");
		}

		private void ImportMath(AssemblyDefinition assembly)
		{
			TypesReferences = new Dictionary<KnownMathFunctionType, MethodReference>();
			foreach (var typeMethod in KnownMathFunction.TypesMethods)
				TypesReferences.Add(typeMethod.Key, assembly.MainModule.Import(typeMethod.Value));
		}

		private void DefineLocals()
		{
			LocalVarNumber = 0;
			FuncNodes = new Dictionary<FuncNode, CountNumber>();
			DefineLocals(Root);

			InvertNumbers(Root);
		}

		private void DefineLocals(MathFuncNode node)
		{
			var funcNode = node as FuncNode;
			if (funcNode == null)
				return;

			if (FuncNodes.ContainsKey(funcNode))
			{
				funcNode.Number = FuncNodes[funcNode].Number;
				FuncNodes[funcNode].Count += 1;
			}
			else
			{
				FuncNodes.Add(funcNode, new CountNumber() { Number = LocalVarNumber, Count = 1, Calculated = false });
				funcNode.Number = LocalVarNumber;
				LocalVarNumber += funcNode.Childs.Count;
			}

			foreach (var child in node.Childs)
				DefineLocals(child);
		}

		private void InvertNumbers(MathFuncNode node)
		{
			var funcNode = node as FuncNode;
			if (funcNode != null)
			{
				funcNode.Number = LocalVarNumber - funcNode.Number - funcNode.Childs.Count;
				FuncNodes[funcNode].Number = funcNode.Number;

				foreach (var child in funcNode.Childs)
					InvertNumbers(child);
			}
		}

		private void EmitNode(MathFuncNode node)
		{
			switch (node.Type)
			{
				case MathNodeType.Value:
					IlInstructions.Add(new OpCodeArg(OpCodes.Ldc_R8, node.Value.ToDouble()));
					break;

				case MathNodeType.Constant:
				case MathNodeType.Variable:
					IlInstructions.Add(new OpCodeArg(OpCodes.Ldarg, node.Number + 1));
					break;

				case MathNodeType.Function:
					var funcNode = node as FuncNode;
					var func = FuncNodes[funcNode];
					if (!func.Calculated)
					{
						EmitFunc(funcNode);
						func.Calculated = true;
					}
					else
						IlInstructions.Add(new OpCodeArg(OpCodes.Ldloc, funcNode.Number));
					break;
			}
		}

		private void EmitFunc(FuncNode funcNode)
		{
			switch (funcNode.FunctionType)
			{
				case KnownMathFunctionType.Add:
					EmitAddMultFunc(funcNode, OpCodes.Add);
					return;
				case KnownMathFunctionType.Sub:
					EmitAddMultFunc(funcNode, OpCodes.Sub);
					return;
				case KnownMathFunctionType.Mult:
					EmitAddMultFunc(funcNode, OpCodes.Mul);
					return;
				case KnownMathFunctionType.Div:
					EmitAddMultFunc(funcNode, OpCodes.Div);
					return;
				case KnownMathFunctionType.Neg:
					EmitNode(funcNode.Childs[0]);
					IlInstructions.Add(new OpCodeArg(OpCodes.Neg));
					if (FuncNodes[funcNode].Count > 1)
					{
						IlInstructions.Add(new OpCodeArg(OpCodes.Stloc, funcNode.Number));
						IlInstructions.Add(new OpCodeArg(OpCodes.Ldloc, funcNode.Number));
					}
					return;
				case KnownMathFunctionType.Exp:
					if (funcNode.Childs[1].IsValue && funcNode.Childs[1].Value.IsInteger)
					{
						int power = (int)funcNode.Childs[1].Value.Numerator;
						
						EmitNode(funcNode.Childs[0]);

						if (power <= 3)
						{
							IlInstructions.Add(new OpCodeArg(OpCodes.Stloc, funcNode.Number));
							IlInstructions.Add(new OpCodeArg(OpCodes.Ldloc, funcNode.Number));
							for (int i = 1; i < power; i++)
							{
								IlInstructions.Add(new OpCodeArg(OpCodes.Ldloc, funcNode.Number));
								IlInstructions.Add(new OpCodeArg(OpCodes.Mul));
							}
						}
						else if (power == 4)
						{
							IlInstructions.Add(new OpCodeArg(OpCodes.Stloc, funcNode.Number));
							IlInstructions.Add(new OpCodeArg(OpCodes.Ldloc, funcNode.Number));
							IlInstructions.Add(new OpCodeArg(OpCodes.Ldloc, funcNode.Number));
							IlInstructions.Add(new OpCodeArg(OpCodes.Mul));
							IlInstructions.Add(new OpCodeArg(OpCodes.Stloc, funcNode.Number));
							IlInstructions.Add(new OpCodeArg(OpCodes.Ldloc, funcNode.Number));
							IlInstructions.Add(new OpCodeArg(OpCodes.Ldloc, funcNode.Number));
							IlInstructions.Add(new OpCodeArg(OpCodes.Mul));
						}
						else
						{
							// result: funcNode.Number
							// x: funcNode.Number + 1

							//int result = x;
							IlInstructions.Add(new OpCodeArg(OpCodes.Stloc, funcNode.Number + 1));
							IlInstructions.Add(new OpCodeArg(OpCodes.Ldloc, funcNode.Number + 1));

							power--;
							do
							{
								if ((power & 1) == 1)
								{
									IlInstructions.Add(new OpCodeArg(OpCodes.Ldloc, funcNode.Number + 1));
									IlInstructions.Add(new OpCodeArg(OpCodes.Mul));
								}

								//x = x * x;
								if (power <= 1)
									break;

								IlInstructions.Add(new OpCodeArg(OpCodes.Ldloc, funcNode.Number + 1));
								IlInstructions.Add(new OpCodeArg(OpCodes.Ldloc, funcNode.Number + 1));
								IlInstructions.Add(new OpCodeArg(OpCodes.Mul));
								IlInstructions.Add(new OpCodeArg(OpCodes.Stloc, funcNode.Number + 1));

								power = power >> 1;
							}
							while (power != 0);
						}

						if (FuncNodes[funcNode].Count > 1)
						{
							IlInstructions.Add(new OpCodeArg(OpCodes.Stloc, funcNode.Number));
							IlInstructions.Add(new OpCodeArg(OpCodes.Ldloc, funcNode.Number));
						}
						return;
					}
					break;
			}

			if (KnownMathFunction.TypesMethods.ContainsKey((KnownMathFunctionType)funcNode.FunctionType))
			{
				foreach (var child in funcNode.Childs)
					EmitNode(child);
				IlInstructions.Add(new OpCodeArg(OpCodes.Call, TypesReferences[(KnownMathFunctionType)funcNode.FunctionType]));
				if (FuncNodes[funcNode].Count > 1)
				{
					IlInstructions.Add(new OpCodeArg(OpCodes.Stloc, funcNode.Number));
					IlInstructions.Add(new OpCodeArg(OpCodes.Ldloc, funcNode.Number));
				}
			}
		}

		private void EmitAddMultFunc(FuncNode funcNode, OpCode opCode)
		{
			EmitNode(funcNode.Childs[0]);
			for (int i = 1; i < funcNode.Childs.Count; i++)
			{
				EmitNode(funcNode.Childs[i]);
				IlInstructions.Add(new OpCodeArg(opCode));
			}
			if (FuncNodes[funcNode].Count > 1)
			{
				IlInstructions.Add(new OpCodeArg(OpCodes.Stloc, funcNode.Number));
				IlInstructions.Add(new OpCodeArg(OpCodes.Ldloc, funcNode.Number));
			}
		}

		private void OptimizeInstructions()
		{
			int i = 0;
			while (i < IlInstructions.Count)
			{
				if (i < IlInstructions.Count - 1)
				{
					var firstOpCode = IlInstructions[i].OpCode;

					if ((firstOpCode == OpCodes.Ldarg || firstOpCode == OpCodes.Ldc_R8) && IlInstructions[i + 1].OpCode == OpCodes.Stloc)
					{
						var locNumber = (int)IlInstructions[i + 1].Arg;

						for (int j = i + 2; j < IlInstructions.Count; j++)
						{
							if ((IlInstructions[j].OpCode == OpCodes.Stloc) && (int)IlInstructions[j].Arg == locNumber)
								break;

							if ((IlInstructions[j].OpCode == OpCodes.Ldloc) && (int)IlInstructions[j].Arg == locNumber)
								IlInstructions[j] = new OpCodeArg(firstOpCode, IlInstructions[i].Arg);
						}

						IlInstructions.RemoveRange(i, 2);
						continue;
					}

					if (firstOpCode == OpCodes.Ldloc && IlInstructions[i + 1].OpCode == OpCodes.Stloc)
						if ((int)IlInstructions[i].Arg == (int)IlInstructions[i + 1].Arg)
						{
							IlInstructions.RemoveRange(i, 2);
							continue;
						}
						else
						{
							var locNumber1 = (int)IlInstructions[i].Arg;
							var locNumber2 = (int)IlInstructions[i + 1].Arg;

							bool arg2IsUsed = false;
							for (int j = i + 2; j < IlInstructions.Count; j++)
							{
								if ((IlInstructions[j].OpCode == OpCodes.Stloc) && (int)IlInstructions[j].Arg == locNumber1)
									break;

								if ((IlInstructions[j].OpCode == OpCodes.Stloc) && (int)IlInstructions[j].Arg == locNumber2)
									break;

								if ((IlInstructions[j].OpCode == OpCodes.Ldloc) && (int)IlInstructions[j].Arg == locNumber2)
								{
									IlInstructions[j] = new OpCodeArg(OpCodes.Ldloc, locNumber1);
									arg2IsUsed = true;
								}
							}

							if (arg2IsUsed)
							{
								IlInstructions.RemoveRange(i, 2);
								continue;
							}
						}
					
					if (firstOpCode == OpCodes.Stloc && IlInstructions[i + 1].OpCode == OpCodes.Ldloc &&
						(int)IlInstructions[i].Arg == (int)IlInstructions[i + 1].Arg)
					{
						var locNumber = (int)IlInstructions[i].Arg;

						bool remove = true;
						for (int j = i + 2; j < IlInstructions.Count; j++)
							if ((IlInstructions[j].OpCode == OpCodes.Ldloc) && (int)IlInstructions[j].Arg == locNumber)
							{
								remove = false;
								break;
							}

						if (remove)
						{
							IlInstructions.RemoveRange(i, 2);
							continue;
						}
					}
				}

				i++;
			}
		}

		private void OptimizeLocalVariables(ref int localVarNumber)
		{
			bool[,] localVaribales = new bool[IlInstructions.Count, localVarNumber];

			List<List<VariableLifetimeCycle>> localVariablesLifeCycles = new List<List<VariableLifetimeCycle>>(localVarNumber);
			for (int i = 0; i < localVarNumber; i++)
				localVariablesLifeCycles.Add(new List<VariableLifetimeCycle>());

			for (int i = 0; i < IlInstructions.Count; i++)
				if (IlInstructions[i].OpCode == OpCodes.Stloc)
					localVariablesLifeCycles[(int)IlInstructions[i].Arg].Add(new VariableLifetimeCycle(i, i));
				else if (IlInstructions[i].OpCode == OpCodes.Ldloc)
				{
					var cycles = localVariablesLifeCycles[(int)IlInstructions[i].Arg];
					cycles[cycles.Count - 1] = new VariableLifetimeCycle(cycles[cycles.Count - 1].BeginInd, i);
				}

			for (int i = 0; i < localVariablesLifeCycles.Count; i++)
				for (int j = 0; j < localVariablesLifeCycles[i].Count; j++)
					for (int k = localVariablesLifeCycles[i][j].BeginInd; k <= localVariablesLifeCycles[i][j].EndInd; k++)
						localVaribales[k, i] = true;

			int localMaxNumber = 0;
			for (int i = 0; i < IlInstructions.Count; i++)
			{
				if (IlInstructions[i].OpCode == OpCodes.Stloc)
				{
					var minNumber = FindMinNumber(localVaribales, i);
					var oldNumber = (int)IlInstructions[i].Arg;
					if (minNumber < oldNumber)
					{
						if (minNumber > localMaxNumber)
							localMaxNumber = minNumber;

						IlInstructions[i] = new OpCodeArg(OpCodes.Stloc, minNumber);

						int j = i;
						while (j < IlInstructions.Count && localVaribales[j, oldNumber])
						{
							localVaribales[j, minNumber] = true;
							localVaribales[j, oldNumber] = false;
							if (IlInstructions[j].OpCode == OpCodes.Ldloc &&
								(int)IlInstructions[j].Arg == oldNumber)
								IlInstructions[j] = new OpCodeArg(OpCodes.Ldloc, minNumber);
							j++;
						}
					}
				}
			}

			localVarNumber = localMaxNumber + 1;
		}

		private static int FindMinNumber(bool[,] localVaribales, int ind)
		{
			int result = 0;
			for (int i = 0; i < localVaribales.GetLength(1); i++)
				if (localVaribales[ind, i] == false)
				{
					result = i;
					break;
				}

			return result;
		}

		private static void EmitInstruction(ILProcessor ilProcessor, OpCodeArg instr)
		{
			if (instr.Arg == null)
				ilProcessor.Emit(instr.OpCode);
			else if (instr.Arg is int)
			{
				if (instr.OpCode == OpCodes.Ldarg)
					EmitArgLoad(ilProcessor, (int)instr.Arg);
				else if (instr.OpCode == OpCodes.Ldloc)
					EmitLocalLoad(ilProcessor, (int)instr.Arg);
				else if (instr.OpCode == OpCodes.Stloc)
					EmitLocalSave(ilProcessor, (int)instr.Arg);
				else
					ilProcessor.Emit(instr.OpCode, (int)instr.Arg);
			}
			else if (instr.Arg is double)
				ilProcessor.Emit(instr.OpCode, (double)instr.Arg);
			else if (instr.Arg is float)
				ilProcessor.Emit(instr.OpCode, (float)instr.Arg);
			else if (instr.Arg is string)
				ilProcessor.Emit(instr.OpCode, (string)instr.Arg);
			else if (instr.Arg is byte)
				ilProcessor.Emit(instr.OpCode, (byte)instr.Arg);
			else if (instr.Arg is sbyte)
				ilProcessor.Emit(instr.OpCode, (sbyte)instr.Arg);
			else if (instr.Arg is CallSite)
				ilProcessor.Emit(instr.OpCode, (CallSite)instr.Arg);
			else if (instr.Arg is FieldReference)
				ilProcessor.Emit(instr.OpCode, (FieldReference)instr.Arg);
			else if (instr.Arg is Instruction)
				ilProcessor.Emit(instr.OpCode, (Instruction)instr.Arg);
			else if (instr.Arg is Instruction[])
				ilProcessor.Emit(instr.OpCode, (Instruction[])instr.Arg);
			else if (instr.Arg is MethodReference)
				ilProcessor.Emit(instr.OpCode, (MethodReference)instr.Arg);
			else if (instr.Arg is ParameterDefinition)
				ilProcessor.Emit(instr.OpCode, (ParameterDefinition)instr.Arg);
			else if (instr.Arg is TypeReference)
				ilProcessor.Emit(instr.OpCode, (TypeReference)instr.Arg);
			else if (instr.Arg is VariableDefinition)
				ilProcessor.Emit(instr.OpCode, (VariableDefinition)instr.Arg);
		}

		public static void EmitArgLoad(ILProcessor ilProcessor, int argNumber)
		{
			switch (argNumber)
			{
				case 0:
					ilProcessor.Emit(OpCodes.Ldarg_0);
					break;
				case 1:
					ilProcessor.Emit(OpCodes.Ldarg_1);
					break;
				case 2:
					ilProcessor.Emit(OpCodes.Ldarg_2);
					break;
				case 3:
					ilProcessor.Emit(OpCodes.Ldarg_3);
					break;
				default:
					if (argNumber < 256)
						ilProcessor.Emit(OpCodes.Ldarg_S, (byte)argNumber);
					else
						ilProcessor.Emit(OpCodes.Ldarg, (ushort)argNumber);
					break;
			}
		}

		public static void EmitLocalLoad(ILProcessor ilProcessor, int localNumber)
		{
			switch (localNumber)
			{
				case 0:
					ilProcessor.Emit(OpCodes.Ldloc_0);
					break;
				case 1:
					ilProcessor.Emit(OpCodes.Ldloc_1);
					break;
				case 2:
					ilProcessor.Emit(OpCodes.Ldloc_2);
					break;
				case 3:
					ilProcessor.Emit(OpCodes.Ldloc_3);
					break;
				default:
					if (localNumber < 256)
						ilProcessor.Emit(OpCodes.Ldloc_S, (byte)localNumber);
					else
						ilProcessor.Emit(OpCodes.Ldloc, (ushort)localNumber);
					break;
			}
		}

		public static void EmitLocalSave(ILProcessor ilProcessor, int localNumber)
		{
			switch (localNumber)
			{
				case 0:
					ilProcessor.Emit(OpCodes.Stloc_0);
					break;
				case 1:
					ilProcessor.Emit(OpCodes.Stloc_1);
					break;
				case 2:
					ilProcessor.Emit(OpCodes.Stloc_2);
					break;
				case 3:
					ilProcessor.Emit(OpCodes.Stloc_3);
					break;
				default:
					if (localNumber < 256)
						ilProcessor.Emit(OpCodes.Stloc_S, (byte)localNumber);
					else
						ilProcessor.Emit(OpCodes.Stloc, (ushort)localNumber);
					break;
			}
		}
	}
}