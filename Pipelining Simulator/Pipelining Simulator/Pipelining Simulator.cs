﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Pipelining_Simulator
{
    public partial class Form1 : Form
    {
        static string path;

        //creating a list of instructions
        List<instruction> instructions = new List<instruction>();
        List<string> InstStrings = new List<string>();
        int PC, ALU, Memory, instruction_fetch, instruction_exec, instruction_decode, instruction_mem, instruction_wb;

        public Form1()
        {
            InitializeComponent();
        }

        void WriteToRegister(int reg, int data)
        {
           this.Controls["Reg" + reg.ToString()].Text= data.ToString();
        }

        int ReadFromRegister(int reg)
        {
            return Convert.ToInt32(this.Controls["Reg" + reg.ToString()].Text);
        }

        void WriteToMemory(int addr, int data)
        {
            this.Controls["Mem" + addr.ToString()].Text = data.ToString();
        }

        int ReadFromMemory(int addr)
        { 
            return Convert.ToInt32(this.Controls["Mem" + addr.ToString()].Text);
        } 

        instruction ParseString(string input)
        {
            instruction temp = new instruction();

            temp.label= input.Substring(0, input.IndexOf(" "));
            //R-format
            if (temp.label == "add" || temp.label == "sub" || temp.label == "or" || temp.label == "slt")
            {
                input = input.Substring(input.IndexOf("$") + 1);
                temp.dest_reg = Convert.ToInt32(input.Substring(0, input.IndexOf(",")));
                input = input.Substring(input.IndexOf("$") + 1);
                temp.first_reg = Convert.ToInt32(input.Substring(0, input.IndexOf(",")));
                input = input.Substring(input.IndexOf("$") + 1);
                temp.second_reg = Convert.ToInt32(input.Substring(0));
            }

            else if (temp.label == "addi")
            {
                input = input.Substring(input.IndexOf("$") + 1);
                temp.second_reg = Convert.ToInt32(input.Substring(0, input.IndexOf(",")));
                input = input.Substring(input.IndexOf("$") + 1);
                temp.first_reg = Convert.ToInt32(input.Substring(0, input.IndexOf(",")));
                input = input.Substring(input.IndexOf(" "));
                temp.immediate = Convert.ToInt32(input);
            }

            else if (temp.label == "lw" || temp.label == "sw" || temp.label == "bne" /*check this and J instruction*/)
            {
                input = input.Substring(input.IndexOf("$") + 1);
                temp.second_reg = Convert.ToInt32(input.Substring(0, input.IndexOf(",")));
                input = input.Substring(input.IndexOf(" ") + 1);
                temp.immediate = Convert.ToInt32(input.Substring(0, input.IndexOf("(")));
                input = input.Substring(input.IndexOf("$") + 1);
                temp.first_reg = Convert.ToInt32(input.Substring(0, input.IndexOf(")")));
            }

            return temp;

        }

        public class instruction
        {
            public int first_reg = 0, second_reg = 0, dest_reg = 0, inst_type = 0, shift_amount = 0, immediate = 0;
            public string label = "";

            public instruction() { }
            public instruction(int _first, int _second, int _dest, string _type, int _shift, int _imm)
            {
                first_reg = _first;
                second_reg = _second;
                dest_reg = _dest;
                shift_amount = _shift;
                immediate = _imm;
                label = _type;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //setting all registers to zero
            for(int i=0; i<16; i++)
                this.Controls["Reg" + i.ToString()].Text = "0"; 
                      
         }

        private void Browse_Click(object sender, EventArgs e)
        {
            //set intial directory to C:\
            openFileDialog.InitialDirectory = @"C:\Users\Mohamed\Desktop";

            // Show the dialog and get result.
            DialogResult result = openFileDialog.ShowDialog();

            if (result == DialogResult.OK) // Test result.
            {
                path = openFileDialog.FileName;
                FileDir.Text = path;
            }

        }
        
        private void SynCheck_Click(object sender, EventArgs e)
        {
            //reading instructions from file and parsing
            foreach (string str in InstStrings)
            {
                instructions.Add(ParseString(str));
            }
        }
        private void Decode(int inst)
        {
            for (int i = 3; i > 1; i--)
            {
                this.Controls["WB" + i.ToString()].Text = this.Controls["WB" + (i - 1).ToString()].Text;
            }
            WB1.Text = "1";
            if (instructions[inst].label[0] == 'b' || instructions[inst].label == "j" || instructions[inst].label == "sw")
                WB1.Text = "0";
            MemRead2.Text = MemRead1.Text;
            if (instructions[inst].label == "lw")
                MemRead1.Text = "1";
            else
                MemRead1.Text = "0";
        }

        private void Execute(int i)
        {
            //NOT FINISHED
            instruction inst = instructions[i];
    
            if (inst.label == "j")
            {
                PC = inst.immediate;
                return;
            }

            if (inst.label[0] == 'b')
            {
                bool equals = (ReadFromRegister(inst.first_reg) == ReadFromRegister(inst.second_reg));

                if ((inst.label == "beq" && equals) || (inst.label == "bne" && !equals))
                {
                    PC += 4 + (inst.immediate * 4);
                }
                return;
            }

            int First = ReadFromRegister(inst.first_reg);
            if (i > 1 && WB2.Text == "1" && (inst.first_reg == instructions[i - 1].dest_reg))
                First = ALU;
            int Second = ReadFromRegister(inst.second_reg);
            if (i > 1 && WB2.Text == "1" && (inst.second_reg == instructions[i - 1].dest_reg))
                Second = ALU;

            if (inst.label == "add")
            {
                ALU = First + Second;
            }

            if (inst.label == "or")
            {
                ALU = First | Second;
            }

            if (inst.label == "slt")
            {
                bool tmp = First < Second;
                ALU = (tmp) ? 1 : 0;
            }

        }
        private void WriteBack (int i)
        {
            if (WB3.Text == "0")
                return;
            if (instructions[i].label == "lw")
                WriteToRegister(instructions[i].dest_reg, Memory);
            else
                WriteToRegister(instructions[i].dest_reg, Convert.ToInt32(ALU3.Text));
        }

        private void Mem(int i)
        {
            //NOT FINISHED
            instruction inst = instructions[i];
            int address = Convert.ToInt32(ALU2.Text);

            if (inst.label == "lw")
            {
                Memory = ReadFromMemory(address);
            }

            if (inst.label == "sw")
            {
                WriteToMemory(address,ReadFromRegister(inst.second_reg));
            }

        }

        private void execute_cycle()
        {
            Execute(instruction_exec);
            Decode(instruction_decode);
            Mem(instruction_mem);
            WriteBack(instruction_wb);
        }

        private void next_cycle()
        {
            instruction_wb = instruction_mem;
            instruction_mem = instruction_exec;
            instruction_exec = instruction_decode;
            instruction_decode = instruction_fetch;
            instruction_fetch = PC/4;
            PC += 4;
            if (instruction_fetch > instructions.Count)
            {
                instruction_fetch = -1;
                PC -= 4;
            }
            ALU3.Text = ALU2.Text;
            ALU2.Text = ALU.ToString();
            Memory3.Text = Memory.ToString();

            if (instruction_wb == -1)
                NextCycle.Enabled = false;

            IFBox.Text = "Instruction " + instruction_fetch.ToString();
            IDBox.Text = "Instruction " + instruction_decode.ToString();
            ExBox.Text = "Instruction " + instruction_exec.ToString();
            MemBox.Text = "Instruction " + instruction_mem.ToString();
            WBBox.Text = "Instruction " + instruction_wb.ToString();
            PCNum.Text = PC.ToString();
            for (int i = 3; i > 0; i--)
            {
                this.Controls["Inst" + i.ToString()].Text = this.Controls["Inst" + (i - 1).ToString()].Text;
            }
            Inst0.Text = (PC / 4).ToString();
            execute_cycle();
        }

        private void LoadBtn_Click(object sender, EventArgs e)
        {
            StreamReader input = new StreamReader(path);
            int InstructionID = 0;
            while (!input.EndOfStream)
            {
                InstStrings.Add(input.ReadLine());
            }
            PC = 0;
            foreach (string str in InstStrings)
            {
                InstructionID++;
                textBox1.AppendText(InstructionID.ToString() + ") " + str + "\n");
            }
            LoadBtn.Enabled = false;
        }

        private void NextCycle_Click(object sender, EventArgs e)
        {
            next_cycle();
        }
    }
}
