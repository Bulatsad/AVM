using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using AVI;

namespace AVM
{
    class AssemblerVirtualMachine
    {
        private IAsseblerVirtualModule[] ModuleList;
        private Dictionary<string, byte> BaitCodeList;
        private Dictionary<int, int> RegisterSizes;
        private Dictionary<string, int> RegisterCodes;
        private Dictionary<string, string> Flags;
        private List<byte> RAM_ = new List<byte>();
        private List<byte> Registers_ = new List<byte>();
        private byte[] RAM;
        private byte[] Registers;
        private void LoadRegisterDefs(string path = "eregisterdefs.arc")
        {
            RegisterCodes = new Dictionary<string, int>();
            RegisterSizes = new Dictionary<int, int>();
            StreamReader reader = new StreamReader(path);
            int maxaddr = 0;
            int maxsize = 0;
            while (!reader.EndOfStream)
            {
                string command = reader.ReadLine();
                string[] args = VMCommands.GetArguments(command);
                if(Convert.ToInt32(args[0], 16) > maxaddr)
                {
                    maxaddr = Convert.ToInt32(args[0], 16);
                    maxsize = Convert.ToInt32(args[1], 10);
                }
                RegisterCodes.Add(command.Substring(0, command.IndexOf(' ')), Convert.ToInt32(args[0], 16));
                RegisterSizes.Add(Convert.ToInt32(args[0], 16), Convert.ToInt32(args[1], 10));
            }
            Registers_.Capacity = maxaddr + maxsize;
        }
        private void LoadBaitCodes(string path = "einstructiondefs.arc")
        {
            BaitCodeList = new Dictionary<string, byte>();
            StreamReader reader = new StreamReader(path);
            int i = 0;
            string instruction;
            while (!reader.EndOfStream)
            {
                instruction = reader.ReadLine();
                VMCommands.ClearCommand(ref instruction);
                BaitCodeList.Add(instruction, Convert.ToByte(i));
                i++;
            }
        }
        private void LoadFlags(string path = "eflags.arc")
        {
            Flags = new Dictionary<string, string>();
            StreamReader reader = new StreamReader(path);
            string line, command;
            string[] args;
            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
                command = line.Substring(0, line.IndexOf(' '));
                args = VMCommands.GetArguments(line);
                switch (command)
                {
                    case "bit_depth": Flags[command] = args[0];break;
                    case "RAM_SIZE": Flags[command] = args[0];break;
                }
            }
        }
        public AssemblerVirtualMachine()
        {
            LoadBaitCodes();
            LoadRegisterDefs();
            LoadFlags();
            ModuleList = Connector.GetConnectedModudels();
            RAM_.Capacity = Convert.ToInt32(Flags["RAM_SIZE"]);
            for (int i = 0; i < RAM_.Capacity; i++)
                RAM_.Add(0);
            for (int i = 0; i < Registers_.Capacity; i++)
                Registers_.Add(0);
            RAM = RAM_.ToArray();
            Registers = Registers_.ToArray();
            foreach (IAsseblerVirtualModule module in ModuleList)
            {
                module.InitExecute(BaitCodeList, RegisterCodes, RegisterSizes, Flags);
            }

        }
        private void LoadProgramToMemory(byte[] program)
        {
            for (int i = 0; i < program.Length; i++)
                RAM[i] = program[i];
        }
        private int ReadReg(int regAddr,int regSize)
        {
            List<byte> byteResult = new List<byte>();
            for (int i = 0; i < regSize; i++)
                byteResult.Add(Registers[regAddr + i]);
            return BitConverter.ToInt32(byteResult.ToArray(), 0);
        }
        public byte Run(string path)
        {
            byte[] program = File.ReadAllBytes(path);
            LoadProgramToMemory(program);
            
            while(ReadReg(RegisterCodes["ip"],RegisterSizes[RegisterCodes["ip"]]) < program.Length)
            {
                foreach (IAsseblerVirtualModule module in ModuleList)
                {
                    if(module.IsExecutable(RAM[ReadReg(RegisterCodes["ip"], RegisterSizes[RegisterCodes["ip"]])]))
                    {
                        module.Execute(ref Registers, ref  RAM);
                    }
                }
            }


            return 0;
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            AssemblerVirtualMachine machine = new AssemblerVirtualMachine();
            machine.Run("C:/Users/Bulat/source/repos/AVC/AVC/bin/Debug/output.binavm");
        }
    }
}
