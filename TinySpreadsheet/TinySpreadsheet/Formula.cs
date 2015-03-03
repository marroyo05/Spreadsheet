﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TinySpreadsheet.Tokenize;


namespace TinySpreadsheet
{
    static class Formula
    {
        private static Regex rgx = new Regex(@"^((-{0,1}\()*((\d|[A-Z]\d+)+[\+\/\-\*])*(-{0,1}\d|[A-Z]\d+)+\)*)([\+\/\-\*](\(*((\d|[A-Z]\d+)+[\+\/\-\*])*-{0,1}(\d|[A-Z]\d+)+\)*))*$");    //Valid Formula regex check
        private static Regex alphaNum = new Regex(@"^[A-Za-z]+[0-9]+$");

        public static Double solve(Cell c)
        {
            String cellFormulaString = c.CellFormula.Replace(" ", "");
            if (rgx.IsMatch(cellFormulaString))
            {
                Queue<FormulaToken> cellFormula = ResolveDependencies(Tokenizer.Tokenize(cellFormulaString));
                Queue<FormulaToken> pfix = postFix(cellFormula); //This should be tokenized somewhere.
                return evaluate(pfix);
            }
            return Double.NaN;
        }

        public static Queue<FormulaToken> ResolveDependencies(Queue<FormulaToken> tokens)
        {
            Queue<FormulaToken> outTokens = new Queue<FormulaToken>();
            while(tokens.Count > 0)
            {
                FormulaToken token = tokens.Dequeue();
                if (alphaNum.IsMatch(token.Token)) //If it's a cell, replace with that cells formula
                {
                    if (token.Token[0] == '-')
                    {
                        Double cellContents = Double.Parse(Tokenizer.ExtractCell(token.Token).cellDisplay);
                        cellContents *= -1;
                        outTokens.Enqueue(new FormulaToken(cellContents.ToString(), Tokenizer.TokenType.NUM));
                    }
                    else
                    {
                        outTokens.Enqueue(new FormulaToken(Tokenizer.ExtractCell(token.Token).cellDisplay, Tokenizer.TokenType.NUM));
                    }
                }
                else
                {
                    outTokens.Enqueue(token);
                }
            }

            return outTokens;
        }

        private static Double evaluate(Queue<FormulaToken> postFixed)
        {
            Stack<String> eval = new Stack<String>();
            while (postFixed.Count > 0)
            {
                double num1;
                double num2;
                double result;
                FormulaToken currentToken = postFixed.Dequeue();
                switch (currentToken.Token)
                {

                    case "*":
                        num1 = Double.Parse(eval.Pop().ToString());
                        num2 = Double.Parse(eval.Pop().ToString());
                        result = num2 * num1;
                        eval.Push(result.ToString());
                        break;
                    case "/":
                        num1 = Double.Parse(eval.Pop().ToString());
                        num2 = Double.Parse(eval.Pop().ToString());
                        if (num2 != 0)
                        {
                            result = num2 / num1;
                            eval.Push(result.ToString());
                        }
                        else
                        {
                            return Double.NaN;
                        }
                        break;
                    case "+":
                        num1 = Double.Parse(eval.Pop().ToString());
                        num2 = Double.Parse(eval.Pop().ToString());
                        result = num2 + num1;
                        eval.Push(result.ToString());
                        break;
                    case "-":
                        num1 = Double.Parse(eval.Pop().ToString());
                        num2 = Double.Parse(eval.Pop().ToString());
                        result = num2 - num1;
                        eval.Push(result.ToString());
                        break;
                    default:
                        eval.Push(currentToken.Token);
                        break;
                }
            }
            return Double.Parse(eval.Pop());
        }

        private static Queue<FormulaToken> postFix(Queue<FormulaToken> infix)
        {
            Queue<FormulaToken> output = new Queue<FormulaToken>();
            Stack<FormulaToken> stack = new Stack<FormulaToken>();

            int topPrio = 0;
            while (infix.Count > 0)
            {
                Console.WriteLine(output.ToString());
                FormulaToken currentToken = infix.Dequeue();
                int currPrio = priority(currentToken);
                if (!stack.isEmpty())
                {
                    topPrio = priority(stack.Peek());
                }

                if (currPrio == 1) // A digit or Cell
                {
                    output.Enqueue(currentToken);
                }
                else if (currentToken.Token == "(") //Handle parentheses with recursion
                {
                    //We want the NEXT character, not this left banana.
                    currentToken = infix.Dequeue();
                    //Copy the Parenthesed equation
                    Queue<FormulaToken> tmp = new Queue<FormulaToken>();
                    while (currentToken.Token != ")")
                    {
                        tmp.Enqueue(currentToken);
                        currentToken = infix.Dequeue();
                    }
                    output.Append(postFix(tmp));
                }
                else if (topPrio >= currPrio) //Higher priority operator
                {
                    while (topPrio >= currPrio && !(stack.isEmpty()))
                    {
                        output.Enqueue(stack.Pop());
                        if (!stack.isEmpty())
                        {
                            topPrio = priority(stack.Peek());
                        }
                    }
                    stack.Push(currentToken);
                }
                else //Normal priority operator, move along.
                {
                    stack.Push(currentToken);
                }

            }

            while (!stack.isEmpty())
            {
                output.Enqueue(stack.Pop());
            }
            return output;
        }

        private static int priority(FormulaToken op)
        {
            switch (op.Token)
            {
                case "(":
                case ")":
                    return 4;
                case "*":
                case "/":
                    return 3;
                case "+":
                case "-":
                    return 2;
                default:
                    return 1;
            }
        }

    }
}
