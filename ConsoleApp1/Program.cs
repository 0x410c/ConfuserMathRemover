using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    
    class Program
    {
        static ModuleDefMD targetModule;

        static byte[] data;
        static void Main(string[] args)
        {


            targetModule = ModuleDefMD.Load("./patched-cleaned.bin");
            
            RemoveControlFlowFlattening();
            
            targetModule.Write("./patched.bin");

            Console.ReadLine();
        }

        static void RemoveControlFlowFlattening()
        {
            //iterate all the functions
            foreach(var type in targetModule.Types)
            {
                //Console.WriteLine("Analysing Module :" + type.FullName);
                foreach(var method in type.Methods)
                {
                    RemoveMathConstants(method);
                }
            }
        }

        static List<String> mathConstants = new List<string>
        {
            "System.Double System.Math::Abs(System.Double)",
            "System.Double System.Math::Log(System.Double)",
            "System.Double System.Math::Round(System.Double)",
            "System.Double System.Math::Log10(System.Double)"
        }; 

        static void RemoveMathConstants(MethodDef m)
        {
            if (m.Body == null)
                return;
            for (var i=0;i<m.Body.Instructions.Count; i++)
            {
               
                var instruction = m.Body.Instructions[i];
                var lastInstruction = i==0?Instruction.Create(OpCodes.Nop):m.Body.Instructions[i-1];
                int lstInstrIndex = i - 1;
                if (instruction.OpCode == OpCodes.Call && lastInstruction.OpCode == OpCodes.Ldc_R8) //stores a value in local variable
                {
                    //  Instruction lastInstruction;


                    double value;
                    switch (instruction.Operand.ToString())
                    {
                        case "System.Double System.Math::Abs(System.Double)":
                            value = Math.Abs((Double)lastInstruction.Operand);
                            Console.WriteLine("Found Math junk: " + instruction.ToString());
                            break;
                        case "System.Double System.Math::Log(System.Double)":
                            value = Math.Log((Double)lastInstruction.Operand);
                            Console.WriteLine("Found Math junk: " + instruction.ToString());
                            break;
                        case "System.Double System.Math::Round(System.Double)":
                            value = Math.Round((Double)lastInstruction.Operand);
                            Console.WriteLine("Found Math junk: " + instruction.ToString());
                            break;
                        case "System.Double System.Math::Log10(System.Double)":
                            value = Math.Log10((Double)lastInstruction.Operand);
                            Console.WriteLine("Found Math junk: " + instruction.ToString());
                            break;
                        case "System.Double System.Math::Sqrt(System.Double)":
                            value = Math.Log10((Double)lastInstruction.Operand);
                            Console.WriteLine("Found Math junk: " + instruction.ToString());
                            break;
                        default:
                            continue;
                            break;
                    }
                    //patch the fucking junk
                    //m.Body.Instructions[lstInstrIndex] = Instruction.Create(OpCodes.Nop);
                    m.Body.Instructions[lstInstrIndex].Operand = value;
                    m.Body.UpdateInstructionOffsets();
                    m.Body.Instructions[i] = Instruction.Create(OpCodes.Nop);

                    // m.Body.Instructions[i]
                    m.Body.OptimizeBranches();
                    
                    m.Body.SimplifyBranches();
                }
            }
        }

      

        static int FindInstructionCount(MethodDef method, OpCode opCode, object operand)
        {
            var num = 0;
            if (method.Body != null)
                foreach (var instruction in method.Body.Instructions)
                {
                    if (instruction.OpCode != opCode)
                        continue;
                    if (operand is int)
                    {
                        var value = instruction.GetLdcI4Value();
                        if (value == (int)operand)
                            num++;
                    }
                    else if (operand is string)
                    {
                        var value = instruction.Operand.ToString();
                        if (value.Contains(operand.ToString()))
                            num++;
                    }
                }
            return num;
        }
    }
}
