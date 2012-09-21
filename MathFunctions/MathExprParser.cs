﻿//Generated by the GOLD Parser Builder

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using GOLD;
using MathFunctions;

public class MathExprParser
{
	private static Parser parser = new Parser();

	private enum SymbolIndex
	{
		@Eof = 0,                                  // (EOF)
		@Error = 1,                                // (Error)
		@Whitespace = 2,                           // Whitespace
		@Apost = 3,                                // ''
		@Lparan = 4,                               // '('
		@Rparan = 5,                               // ')'
		@Comma = 6,                                // ','
		@Dot = 7,                                  // '.'
		@Semi = 8,                                 // ';'
		@Caret = 9,                                // '^'
		@Pipe = 10,                                // '|'
		@Eq = 11,                                  // '='
		@Addliteral = 12,                          // AddLiteral
		@Id = 13,                                  // Id
		@Multliteral = 14,                         // MultLiteral
		@Number1 = 15,                             // 'Number1'
		@Number2 = 16,                             // 'Number2'
		@Addition = 17,                            // <Addition>
		@Devider = 18,                             // <Devider>
		@Exponentiation = 19,                      // <Exponentiation>
		@Expression = 20,                          // <Expression>
		@Expressionlist = 21,                      // <ExpressionList>
		@Funcdef = 22,                             // <FuncDef>
		@Multiplication = 23,                      // <Multiplication>
		@Negation = 24,                            // <Negation>
		@Statement = 25,                           // <Statement>
		@Statements = 26,                          // <Statements>
		@Value = 27                                // <Value>
	}

	private enum ProductionIndex
	{
		@Statements = 0,                           // <Statements> ::= <Statements> <Devider> <Statement>
		@Statements2 = 1,                          // <Statements> ::= <Statements> <Devider>
		@Statements3 = 2,                          // <Statements> ::= <Statement>
		@Devider_Semi = 3,                         // <Devider> ::= ';'
		@Devider_Dot = 4,                          // <Devider> ::= '.'
		@Statement_Eq = 5,                         // <Statement> ::= <Expression> '=' <Expression>
		@Statement = 6,                            // <Statement> ::= <Expression>
		@Expression = 7,                           // <Expression> ::= <FuncDef>
		@Expression2 = 8,                          // <Expression> ::= <Addition>
		@Funcdef_Id_Lparan_Rparan = 9,             // <FuncDef> ::= Id '(' <ExpressionList> ')'
		@Funcdef_Id_Apost_Lparan_Rparan = 10,      // <FuncDef> ::= Id '' '(' <ExpressionList> ')'
		@Funcdef_Id_Lparan_Rparan_Apost = 11,      // <FuncDef> ::= Id '(' <ExpressionList> ')' ''
		@Expressionlist_Comma = 12,                // <ExpressionList> ::= <ExpressionList> ',' <Expression>
		@Expressionlist = 13,                      // <ExpressionList> ::= <Expression>
		@Addition_Addliteral = 14,                 // <Addition> ::= <Addition> AddLiteral <Multiplication>
		@Addition_Addliteral2 = 15,                // <Addition> ::= <Addition> AddLiteral <FuncDef>
		@Addition_Addliteral3 = 16,                // <Addition> ::= <FuncDef> AddLiteral <Multiplication>
		@Addition_Addliteral4 = 17,                // <Addition> ::= <FuncDef> AddLiteral <FuncDef>
		@Addition = 18,                            // <Addition> ::= <Multiplication>
		@Multiplication_Multliteral = 19,          // <Multiplication> ::= <Multiplication> MultLiteral <Exponentiation>
		@Multiplication_Multliteral2 = 20,         // <Multiplication> ::= <Multiplication> MultLiteral <FuncDef>
		@Multiplication_Multliteral3 = 21,         // <Multiplication> ::= <FuncDef> MultLiteral <Exponentiation>
		@Multiplication_Multliteral4 = 22,         // <Multiplication> ::= <FuncDef> MultLiteral <FuncDef>
		@Multiplication = 23,                      // <Multiplication> ::= <Exponentiation>
		@Exponentiation_Caret = 24,                // <Exponentiation> ::= <Exponentiation> '^' <Negation>
		@Exponentiation_Caret2 = 25,               // <Exponentiation> ::= <Exponentiation> '^' <FuncDef>
		@Exponentiation_Caret3 = 26,               // <Exponentiation> ::= <FuncDef> '^' <Negation>
		@Exponentiation_Caret4 = 27,               // <Exponentiation> ::= <FuncDef> '^' <FuncDef>
		@Exponentiation = 28,                      // <Exponentiation> ::= <Negation>
		@Negation_Addliteral = 29,                 // <Negation> ::= AddLiteral <Value>
		@Negation_Addliteral2 = 30,                // <Negation> ::= AddLiteral <FuncDef>
		@Negation = 31,                            // <Negation> ::= <Value>
		@Value_Id = 32,                            // <Value> ::= Id
		@Value_Number1 = 33,                       // <Value> ::= 'Number1'
		@Value_Number2 = 34,                       // <Value> ::= 'Number2'
		@Value_Lparan_Rparan = 35,                 // <Value> ::= '(' <Expression> ')'
		@Value_Pipe_Pipe = 36,                     // <Value> ::= '|' <Expression> '|'
		@Value_Lparan_Rparan_Apost = 37,           // <Value> ::= '(' <Expression> ')' ''
		@Value_Pipe_Pipe_Apost = 38,               // <Value> ::= '|' <Expression> '|' ''
		@Value_Id_Apost = 39                       // <Value> ::= Id ''
	}

	public Reduction Root;

	public List<ParserError> Errors;

	private List<int> ArgsCount;
	private List<KnownMathFunctionType?> ArgsFuncTypes;
	private Stack<MathFuncNode> Args;
	private Stack<MathFunc> Funcs;

	protected Dictionary<string, ConstNode> Parameters;

	public IList<MathFunc> Statements
	{
		get;
		protected set;
	}

	public MathFunc FirstStatement
	{
		get
		{
			return Statements.First();
		}
	}

	public static bool AdditionMultiChilds
	{
		get;
		set;
	}

	public static bool MultiplicationMultiChilds
	{
		get;
		set;
	}

	static MathExprParser()
	{
		//This procedure can be called to load the parse tables. The class can
		//read tables using a BinaryReader.
		AdditionMultiChilds = true;
		MultiplicationMultiChilds = true;
		parser.LoadTables(new BinaryReader(new MemoryStream(MathFunctions.Properties.Resources.MathExpr)));
	}

	public bool Parse(string str)
	{
		return Parse(new StreamReader(new MemoryStream(Encoding.Default.GetBytes(str))));
	}

	public bool Parse(TextReader reader)
	{
		//This procedure starts the GOLD Parser Engine and handles each of the
		//messages it returns. Each time a reduction is made, you can create new
		//custom object and reassign the .CurrentReduction property. Otherwise,
		//the system will use the Reduction object that was returned.
		//
		//The resulting tree will be a pure representation of the language
		//and will be ready to implement.

		Errors = new List<ParserError>();

		ArgsCount = new List<int>();
		Args = new Stack<MathFuncNode>();
		ArgsFuncTypes = new List<KnownMathFunctionType?>();
		Parameters = new Dictionary<string, ConstNode>();
		Funcs = new Stack<MathFunc>();

		Statements = new List<MathFunc>();

		bool done;//Controls when we leave the loop
		bool accepted = false;          //Was the parse successful?

		parser.Open(reader);
		parser.TrimReductions = false;  //Please read about this feature before enabling

		done = false;
		while (!done)
		{
			var response = parser.Parse();

			switch (response)
			{
				case ParseMessage.LexicalError:
					//Cannot recognize token
					Errors.Add(new ParserError(parser.CurrentToken().Position(), string.Format("Lexical Error. Token {1} was not expected.", parser.CurrentToken().Data)));
					done = true;
					break;

				case ParseMessage.SyntaxError:
					//Expecting a different token
					Errors.Add(new ParserError(parser.CurrentPosition(), string.Format("Syntax Error. Expecting: {0}.", parser.ExpectedSymbols().Text())));
					done = true;
					break;

				case ParseMessage.Reduction:
					//Create a customized object to store the reduction
					CreateNewObject((Reduction)parser.CurrentReduction);
					break;

				case ParseMessage.Accept:
					//Accepted!
					Root = (Reduction)parser.CurrentReduction;
					done = true;
					accepted = true;
					break;

				case ParseMessage.TokenRead:
					//You don't have to do anything here.
					break;

				case ParseMessage.InternalError:
					//INTERNAL ERROR! Something is horribly wrong.
					Errors.Add(new ParserError("Internal Error. Something is horribly wrong."));
					done = true;
					break;

				case ParseMessage.NotLoadedError:
					//This error occurs if the CGT was not loaded.
					Errors.Add(new ParserError("Grammar Table is not loaded."));
					done = true;
					break;

				case ParseMessage.GroupError:
					//GROUP ERROR! Unexpected end of file
					Errors.Add(new ParserError(parser.CurrentPosition(), "GROUP ERROR! Unexpected end of file."));
					done = true;
					break;
			}
		} //while

		while (Funcs.Count != 0)
			Statements.Add(Funcs.Pop());

		return accepted;
	}

	private void CreateNewObject(Reduction r)
	{
		MathFuncNode arg1, arg2;

		var tableIndex = (ProductionIndex)r.Parent.TableIndex();
		switch (tableIndex)
		{
			case ProductionIndex.Statements:
				// <Statements> ::= <Statements> <Devider> <Statement>
				break;

			case ProductionIndex.Statements2:
				// <Statements> ::= <Statements> <Devider>
				break;

			case ProductionIndex.Statements3:
				// <Statements> ::= <Statement>
				//Funcs.Push(new MathFunc(Args.Pop()));
				break;

			case ProductionIndex.Devider_Semi:
				// <Devider> ::= ';'
				break;

			case ProductionIndex.Devider_Dot:
				// <Devider> ::= '.'
				break;

			case ProductionIndex.Statement_Eq:
				// <Statement> ::= <Expression> '=' <Expression>
				arg2 = Args.Pop();
				arg1 = Args.Pop();
				Funcs.Push(new MathFunc(arg1, arg2, null, Parameters.Select(p => p.Value)));
				Parameters.Clear();
				break;

			case ProductionIndex.Statement:
				// <Statement> ::= <Expression>
				Funcs.Push(new MathFunc(Args.Pop()));
				break;

			case ProductionIndex.Expression:
				// <Expression> ::= <FuncDef>
				break;

			case ProductionIndex.Funcdef_Id_Lparan_Rparan:
				// <FuncDef> ::= Id '(' <ExpressionList> ')'

				PushFunction(r[0].Data.ToString());
				break;

			case ProductionIndex.Funcdef_Id_Apost_Lparan_Rparan:
			// <FuncDef> ::= Id '' '(' <ExpressionList> ')'

			case ProductionIndex.Funcdef_Id_Lparan_Rparan_Apost:

				// <FuncDef> ::= Id '(' <ExpressionList> ')' ''

				PushFunction(r[0].Data.ToString());
				Args.Push(new FuncNode(KnownMathFunctionType.Diff, new MathFuncNode[] { Args.Pop(), null }));

				break;

			case ProductionIndex.Expressionlist_Comma:
				ArgsCount[ArgsCount.Count - 1]++;
				break;

			case ProductionIndex.Expressionlist:
				ArgsCount.Add(1);
				ArgsFuncTypes.Add(null);
				break;

			case ProductionIndex.Expression2:

				// <Expression> ::= <Addition>
				if (AdditionMultiChilds)
					PushOrRemoveFunc(KnownMathFunctionType.Add);

				break;

			case ProductionIndex.Addition_Addliteral:

				// <Addition> ::= <Addition> AddLiteral <Multiplication>
				if (MultiplicationMultiChilds)
					PushOrRemoveFunc(KnownMathFunctionType.Mult);

				if (AdditionMultiChilds)
				{
					ArgsCount[ArgsCount.Count - 1]++;
					if (KnownMathFunction.BinaryNamesFuncs[r[1].Data.ToString()] == KnownMathFunctionType.Sub)
						Args.Push(new FuncNode(KnownMathFunctionType.Neg, new MathFuncNode[] { Args.Pop() }));
				}
				else
					PushBinaryFunction(r[1].Data.ToString());
				break;

			case ProductionIndex.Addition_Addliteral2:

				// <Addition> ::= <Addition> AddLiteral <FuncDef>

				if (AdditionMultiChilds)
				{
					ArgsCount[ArgsCount.Count - 1]++;
					if (KnownMathFunction.BinaryNamesFuncs[r[1].Data.ToString()] == KnownMathFunctionType.Sub)
						Args.Push(new FuncNode(KnownMathFunctionType.Neg, new MathFuncNode[] { Args.Pop() }));
				}
				else
					PushBinaryFunction(r[1].Data.ToString());
				break;

			case ProductionIndex.Addition_Addliteral3:

				// <Addition> ::= <FuncDef> AddLiteral <Multiplication>
				if (MultiplicationMultiChilds)
					PushOrRemoveFunc(KnownMathFunctionType.Mult);

				if (AdditionMultiChilds)
				{
					PushFunc(KnownMathFunctionType.Add, 2);
					if (KnownMathFunction.BinaryNamesFuncs[r[1].Data.ToString()] == KnownMathFunctionType.Sub)

						Args.Push(new FuncNode(KnownMathFunctionType.Neg, new MathFuncNode[] { Args.Pop() }));
				}
				else
					PushBinaryFunction(r[1].Data.ToString());
				break;

			case ProductionIndex.Addition_Addliteral4:

				// <Addition> ::= <FuncDef> AddLiteral <FuncDef>

				if (AdditionMultiChilds)
				{
					PushFunc(KnownMathFunctionType.Add, 2);
					if (KnownMathFunction.BinaryNamesFuncs[r[1].Data.ToString()] == KnownMathFunctionType.Sub)
						Args.Push(new FuncNode(KnownMathFunctionType.Neg, new MathFuncNode[] { Args.Pop() }));
				}
				else
					PushBinaryFunction(r[1].Data.ToString());
				break;

			case ProductionIndex.Addition:

				// <Addition> ::= <Multiplication>

				if (MultiplicationMultiChilds)
					PushOrRemoveFunc(KnownMathFunctionType.Mult);

				if (AdditionMultiChilds)
					PushFunc(KnownMathFunctionType.Add);

				break;

			case ProductionIndex.Multiplication_Multliteral:

			// <Multiplication> ::= <Multiplication> MultLiteral <Exponentiation>
			case ProductionIndex.Multiplication_Multliteral2:

				// <Multiplication> ::= <Multiplication> MultLiteral <FuncDef>

				if (MultiplicationMultiChilds)
				{
					if (KnownMathFunction.BinaryNamesFuncs[r[1].Data.ToString()] == KnownMathFunctionType.Div)
						Args.Push(new FuncNode(KnownMathFunctionType.Exp, new MathFuncNode[] { Args.Pop(), new ValueNode(-1) }));
					ArgsCount[ArgsCount.Count - 1]++;

					/*arg2 = Args.Pop();
					arg1 = Args.Pop();
					bool isMult = KnownMathFunction.BinaryNamesFuncs[r[1].Data.ToString()] == KnownMathFunctionType.Mult;

					if (arg1.Type == MathNodeType.Value && arg2.Type == MathNodeType.Value)
					{
						var arg = isMult ? arg1.Value * arg2.Value : arg1.Value / arg2.Value;
						Args.Push(new ValueNode(new Rational<long>(arg.Numerator, arg.Denominator)));
					}
					else
					{
						Args.Push(arg1);
						if (!isMult)
							Args.Push(new FuncNode(KnownMathFunctionType.Exp, new MathFunctionNode[] { arg2, new ValueNode(-1) }));
						ArgsCount[ArgsCount.Count - 1]++;
					}*/
				}
				else
					PushBinaryFunction(r[1].Data.ToString());
				break;

			case ProductionIndex.Multiplication:

				// <Multiplication> ::= <Exponentiation>

				if (MultiplicationMultiChilds)
					PushFunc(KnownMathFunctionType.Mult);
				break;

			case ProductionIndex.Multiplication_Multliteral3:

			// <Multiplication> ::= <FuncDef> MultLiteral <Exponentiation>
			case ProductionIndex.Multiplication_Multliteral4:

				// <Multiplication> ::= <FuncDef> MultLiteral <FuncDef>

				if (MultiplicationMultiChilds)
				{
					PushFunc(KnownMathFunctionType.Mult, 2);

					if (KnownMathFunction.BinaryNamesFuncs[r[1].Data.ToString()] == KnownMathFunctionType.Div)
						Args.Push(new FuncNode(KnownMathFunctionType.Exp, new MathFuncNode[] { Args.Pop(), new ValueNode(-1) }));

					/*if (KnownMathFunction.BinaryNamesFuncs[r[1].Data.ToString()] == KnownMathFunctionType.Div)
					{
						arg2 = Args.Pop();
						if (arg2.IsValue)
							Args.Push(new ValueNode(new Rational<long>(arg2.Value.Denominator, arg2.Value.Numerator)));
						else
							Args.Push(new FuncNode(KnownMathFunctionType.Exp, new MathFunctionNode[] { arg2, new ValueNode(-1) }));
					}*/
				}
				else
					PushBinaryFunction(r[1].Data.ToString());
				break;

			case ProductionIndex.Exponentiation_Caret:

			// <Exponentiation> ::= <Exponentiation> '^' <Negation>
			case ProductionIndex.Exponentiation_Caret2:

			// <Exponentiation> ::= <Exponentiation> '^' <FuncDef>
			case ProductionIndex.Exponentiation_Caret3:

			// <Exponentiation> ::= <FuncDef> '^' <Negation>
			case ProductionIndex.Exponentiation_Caret4:

				// <Exponentiation> ::= <FuncDef> '^' <FuncDef>

				PushBinaryFunction(r[1].Data.ToString());
				break;

			case ProductionIndex.Exponentiation:

				// <Exponentiation> ::= <Negation>
				break;

			case ProductionIndex.Negation_Addliteral:

			// <Negation> ::= AddLiteral <Value>
			case ProductionIndex.Negation_Addliteral2:

				// <Negation> ::= AddLiteral <FuncDef>
				if (Args.Peek().Type == MathNodeType.Value)
				{
					if (r[0].Data.ToString() == "-")
						Args.Push(new ValueNode(-((ValueNode)Args.Pop()).Value));
				}
				else
					Args.Push(new FuncNode(r[0].Data.ToString(), new MathFuncNode[] { Args.Pop() }));
				break;

			case ProductionIndex.Negation:

				// <Negation> ::= <Value>
				break;

			case ProductionIndex.Value_Id:

				// <Value> ::= Id
				var id = r[0].Data.ToString();
				if (Parameters.ContainsKey(id))
					Args.Push(Parameters[id]);
				else
				{
					var newConst = new ConstNode(id);
					Args.Push(newConst);
					Parameters.Add(id, newConst);
				}
				break;

			case ProductionIndex.Value_Number1:
				// <Value> ::= 'Number1'
			case ProductionIndex.Value_Number2:
				// <Value> ::= 'Number2'

				try
				{
					var str = r[0].Data.ToString();
					var dotInd = str.IndexOf('.');
					string intPart = dotInd == 0 ? "0" : str.Substring(0, dotInd == -1 ? str.Length : dotInd);
					string fracPart = null;
					string periodPart = null;
					if (dotInd != -1)
					{
						int braceInd = str.IndexOf('(', dotInd + 1);
						if (braceInd == -1)
							fracPart = str.Substring(dotInd + 1, str.Length - dotInd - 1);
						else
						{
							fracPart = str.Substring(dotInd + 1, braceInd - dotInd - 1);
							periodPart = str.Substring(braceInd + 1, str.Length - braceInd - 2);
						}
					}
					var result = Rational<long>.FromDecimal(intPart, fracPart, periodPart);
					Args.Push(new ValueNode(result));
				}
				catch
				{
					throw new ArgumentException();
				}
				break;

			case ProductionIndex.Value_Lparan_Rparan:

				// <Value> ::= '(' <Expression> ')'
				break;

			case ProductionIndex.Value_Pipe_Pipe:

				// <Value> ::= '|' <Expression> '|'
				Args.Push(new FuncNode(KnownMathFunctionType.Abs, new MathFuncNode[] { Args.Pop() }));
				break;

			case ProductionIndex.Value_Lparan_Rparan_Apost:

				// <Value> ::= '(' <Expression> ')' ''
				Args.Push(new FuncNode(KnownMathFunctionType.Diff, new MathFuncNode[] { Args.Pop(), null }));
				break;

			case ProductionIndex.Value_Pipe_Pipe_Apost:

				// <Value> ::= '|' <Expression> '|' ''
				Args.Push(new FuncNode(KnownMathFunctionType.Diff, new MathFuncNode[] {
						new FuncNode(KnownMathFunctionType.Abs, new MathFuncNode[] { Args.Pop() }), null }));
				break;

			/*case ProductionIndex.Value_Id_Apost:

				// <Value> ::= Id ''
				Args.Push(new FuncNode(KnownMathFunctionType.Diff, null,
					new MathFunctionNode[] {
						new FuncNode(r[0].Data.ToString(), null, new MathFunctionNode[] { null }), null }));
				break;*/
		}  //switch
	}

	protected void PushFunc(KnownMathFunctionType funcType, int argCount = 1)
	{
		ArgsCount.Add(argCount);
		ArgsFuncTypes.Add(funcType);
	}

	protected void PushOrRemoveFunc(KnownMathFunctionType funcType)
	{
		if (ArgsCount[ArgsCount.Count - 1] == 1 && ArgsFuncTypes[ArgsFuncTypes.Count - 1] == funcType)
		{
			ArgsCount.RemoveAt(ArgsCount.Count - 1);
			ArgsFuncTypes.RemoveAt(ArgsFuncTypes.Count - 1);
		}
		else
			PushFunction(KnownMathFunction.BinaryFuncsNames[funcType]);
	}

	protected void PushBinaryFunction(string mathFunction)
	{
		var arg2 = Args.Pop();
		var arg1 = Args.Pop();
		Args.Push(new FuncNode(mathFunction, new MathFuncNode[] { arg1, arg2 }));
	}

	protected void PushFunction(string mathFunction)
	{
		var args = new MathFuncNode[ArgsCount[ArgsCount.Count - 1]];
		for (int i = 0; i < ArgsCount[ArgsCount.Count - 1]; i++)
			args[ArgsCount[ArgsCount.Count - 1] - 1 - i] = Args.Pop();

		Args.Push(new FuncNode(mathFunction, args));
		ArgsCount.RemoveAt(ArgsCount.Count - 1);
		ArgsFuncTypes.RemoveAt(ArgsFuncTypes.Count - 1);
	}
};