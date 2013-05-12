using System;
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
        static string path;                                          //path for the file to be read
        Dictionary<string, string> Dict;                             //dictionary which contains formats required for error checking
        List<instruction> instructions = new List<instruction>();    //list of parsed instructions
        List<string> InstStrings = new List<string>();               //list of instructions as strings to be output on the screen   
        int PC = -4, ALU, Memory, instruction_fetch = -1, instruction_exec = -1, instruction_decode = -1 , instruction_mem = -1, instruction_wb = -1, CycleNumber = 0;
        
        public Form1()
        {
            InitializeComponent();
        }

        void WriteToRegister(int reg, int data)
        {
            if (reg == 0)
                return;
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

        //Function to parse each instruction of the MIPS code 
        instruction ParseString(string input)
        {
            instruction temp = new instruction();

            temp.label = input.Substring(0, input.IndexOf(" "));
            
            if (temp.label == "add" || temp.label == "or" || temp.label == "slt")
            {
                input = input.Substring(input.IndexOf("$") + 1);
                temp.dest_reg = Convert.ToInt32(input.Substring(0, input.IndexOf(",")));
                input = input.Substring(input.IndexOf("$") + 1);
                temp.first_reg = Convert.ToInt32(input.Substring(0, input.IndexOf(",")));
                input = input.Substring(input.IndexOf("$") + 1);
                temp.second_reg = Convert.ToInt32(input.Substring(0));
            }

            else if (temp.label == "subi")
            {
                input = input.Substring(input.IndexOf("$") + 1);
                temp.dest_reg = Convert.ToInt32(input.Substring(0, input.IndexOf(",")));
                input = input.Substring(input.IndexOf("$") + 1);
                temp.first_reg = Convert.ToInt32(input.Substring(0, input.IndexOf(",")));
                input = input.Substring(input.IndexOf(" "));
                temp.immediate = Convert.ToInt32(input);
            }

            else if (temp.label == "lw" || temp.label == "sw")
            {
                input = input.Substring(input.IndexOf("$") + 1);
                temp.dest_reg = Convert.ToInt32(input.Substring(0, input.IndexOf(",")));
                input = input.Substring(input.IndexOf(" ") + 1);
                temp.immediate = Convert.ToInt32(input.Substring(0, input.IndexOf("(")));
                input = input.Substring(input.IndexOf("$") + 1);
                temp.first_reg = Convert.ToInt32(input.Substring(0, input.IndexOf(")")));
            }

            else if (temp.label == "beq" || temp.label == "bne")
            {
                input = input.Substring(input.IndexOf("$") + 1);
                temp.first_reg = Convert.ToInt32(input.Substring(0, input.IndexOf(",")));
                input = input.Substring(input.IndexOf("$") + 1);
                temp.second_reg = Convert.ToInt32(input.Substring(0, input.IndexOf(",")));
                input = input.Substring(input.IndexOf(" "));
                temp.immediate = Convert.ToInt32(input);
            }

            else if (temp.label == "j")
            {
                input = input.Substring(input.IndexOf(" ") + 1);
                temp.immediate = Convert.ToInt32(input);
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

        //Initializing the dictionary with the formats for error checking
        private void init_dict()
        {
            Dict = new Dictionary<string,string> ();
            Dict.Add("add","R,R,R");
            Dict.Add("or","R,R,R");
            Dict.Add("slt","R,R,R");
            Dict.Add("subi", "R,R,I");
            Dict.Add("lw", "R,I(R)");
            Dict.Add("sw", "R,I(R)");
            Dict.Add("beq","R,R,I");
            Dict.Add("bne","R,R,I");
            Dict.Add("j","I");
        }
        
        //Function to reset all the data to load a new file
        private void reset_all()
        {
            init_dict();
            foreach (Control s in this.Controls)
            {
                if (s.GetType().ToString() == "System.Windows.Forms.TextBox")
                    s.Text = "";
            }
            //setting all registers to zero
            for (int i = 0; i < 16; i++)
                this.Controls["Reg" + i.ToString()].Text = "0";

            //setting all memory locations to zero
            for (int i = 0; i < 16; i++)
                this.Controls["Mem" + i.ToString()].Text = "0";
            PCNum.Text = "";
            instructions.Clear();
            InstStrings.Clear();
            CycleNum.Text = "";
            PC = -4;
            instruction_fetch = -1;
            instruction_exec = -1; 
            instruction_decode = -1;
            instruction_mem = -1;
            instruction_wb = -1;
            CycleNumber = 0;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            reset_all();              
        }

        private void Browse_Click(object sender, EventArgs e)
        {
            //set intial directory to C:\
            openFileDialog.InitialDirectory = @"C:\";

            // Show the dialog and get result.
            DialogResult result = openFileDialog.ShowDialog();

            if (result == DialogResult.OK) // Test result
            {
                path = openFileDialog.FileName;
                FileDir.Text = path;
                LoadBtn.Enabled = true;
            }
        }
        
        //Generate control signals and check for stalling conditions
        private void Decode(int inst)
        {
            //Return if the instruction is invalid or the last instruction has been executed
            if (inst < 0 || inst >= instructions.Count)
                return;

            //Update write back signals from previous stage to current stage
            for (int i = 3; i > 1; i--)
            {
                this.Controls["WB" + i.ToString()].Text = this.Controls["WB" + (i - 1).ToString()].Text;
            }

            //Control Signals
            WB1.Text = "1";
            if (instructions[inst].label[0] == 'b' || instructions[inst].label == "j" || instructions[inst].label == "sw")
                WB1.Text = "0";
            MemRead2.Text = MemRead1.Text;
            if (instructions[inst].label == "lw")
                MemRead1.Text = "1";
            else
                MemRead1.Text = "0";
            
            //Stall conditions
            if (inst > 0 && instructions[inst - 1].label == "lw")
            {
                if (instructions[inst - 1].dest_reg == instructions[inst].dest_reg && instructions[inst].label == "sw" && instruction_exec == inst - 1)
                {
                    Inst0.Text = inst.ToString();
                    IFBox.Text = "Instruction " + inst.ToString();
                    Inst1.Text =  "-1";
                    IDBox.Text = "Instruction -1";
                    instruction_fetch = inst;
                    instruction_decode = -1;
                    PC -= 4;
                }

                else if ((instructions[inst - 1].dest_reg == instructions[inst].first_reg || instructions[inst - 1].dest_reg == instructions[inst].second_reg) && instruction_exec == inst - 1)
                {
                    Inst0.Text = inst.ToString();
                    IFBox.Text = "Instruction " + inst.ToString();
                    Inst1.Text = "-1";
                    IDBox.Text = "Instruction -1";
                    instruction_fetch = inst;
                    instruction_decode = -1;
                    PC -= 4;
                }
            }

            else if (inst > 0 && instructions[inst - 1].label == "sw")
            {
                if (instructions[inst - 1].dest_reg == instructions[inst].dest_reg && instructions[inst].label == "lw" && instruction_exec == inst - 1)
                {
                    Inst0.Text = inst.ToString();
                    IFBox.Text = "Instruction " + inst.ToString();
                    Inst1.Text = "-1";
                    IDBox.Text = "Instruction -1";
                    instruction_fetch = inst;
                    instruction_decode = -1;
                    PC -= 4;
                }
            }
        }

        //Execute stage with forwarding
        private void Execute(int i)
        {
            //Return if the instruction is invalid or the last instruction has been executed
            if (i < 0 || i >= instructions.Count)
                return;

            instruction inst = instructions[i];
    
            //Forwarding
            int First = ReadFromRegister(inst.first_reg);
            int Second = ReadFromRegister(inst.second_reg);

            if (i > 0 && WB2.Text == "1" && (inst.first_reg == instructions[i - 1].dest_reg) && inst.dest_reg != 0 && inst.first_reg != 0 && instruction_mem != -1)
                First = ALU;
            else if (i > 1 && WB3.Text == "1" && (inst.first_reg == instructions[i - 2].dest_reg) && inst.dest_reg != 0 && inst.first_reg != 0 && instruction_mem != -1 && instructions[i - 2].label != "lw")
                First = Convert.ToInt32(ALU3.Text);
            if (i > 0 && WB2.Text == "1" && (inst.second_reg == instructions[i - 1].dest_reg) && inst.dest_reg != 0 && inst.second_reg != 0 && instruction_mem != -1)
                Second = ALU;
            else if (i > 1 && WB3.Text == "1" && (inst.second_reg == instructions[i - 2].dest_reg) && inst.dest_reg != 0 && inst.second_reg != 0 && instruction_mem != -1 && instructions[i - 2].label != "lw")
                Second = Convert.ToInt32(ALU3.Text);

            //ALU Operations
            if (inst.label == "add")
            {
                ALU = First + Second;
            }

            else if (inst.label == "or")
            {
                ALU = First | Second;
            }

            else if (inst.label == "slt")
            {
                bool tmp = First < Second;
                ALU = (tmp) ? 1 : 0;
            }

            else if (inst.label == "subi")
            {
                ALU = First - inst.immediate;
            }

            else if (inst.label == "lw" || inst.label == "sw")
            {
              ALU = First + inst.immediate;
            }

            //Jump
            else if (inst.label == "j")
            {
                PC = inst.immediate - 4;
                return;
            }
               
            //Branches
            else if (inst.label[0] == 'b')
            {
                //Check if register contents are equal
                bool equals = (ReadFromRegister(inst.first_reg) == ReadFromRegister(inst.second_reg));

                if ((inst.label == "beq" && equals) || (inst.label == "bne" && !equals))
                {
                    PC = (i * 4) + (inst.immediate * 4);
                    instruction_decode = -1;
                    instruction_fetch = -1;
                    Inst0.Text = "-1";
                    Inst1.Text = "-1";
                    IFBox.Text = "Instruction -1";
                    IDBox.Text = "Instruction -1";
                }
                return;
            }

        }

        //Writeback Stage
        private void WriteBack (int i)
        {
            //Return if the instruction is invalid or the last instruction has been executed
            if (i < 0 || i >= instructions.Count)
                return;

            //If there is no write signal return
            if (WB3.Text == "0")
                return;
            string s = instructions[i].label;
            //if the current instruction is a load, write to register from memory
            if (s == "lw")
                WriteToRegister(instructions[i].dest_reg, Memory);
            
            //Write to register from ALU result
            else if(s == "add" || s == "subi" || s == "slt" || s == "or")
                WriteToRegister(instructions[i].dest_reg, Convert.ToInt32(ALU3.Text));
        }

        //Memory Stage
        private void Mem (int i)
        {
            //Return if the instruction is invalid or the last instruction has been executed
            if (i < 0 || i >= instructions.Count)
                return;

            instruction inst = instructions[i];
            int address = Convert.ToInt32(ALU2.Text);  //Get address from EX/MEM register

            //Load word
            if (inst.label == "lw")
            {
                Memory = ReadFromMemory(address);
            }

            //Store word
            if (inst.label == "sw")
            {
                WriteToMemory(address,ReadFromRegister(inst.dest_reg));
            }

        }

        private void execute_cycle()
        {
            WriteBack(instruction_wb);
            Execute(instruction_exec);
            Decode(instruction_decode);
            Mem(instruction_mem);
        }

        private void next_cycle()
        {
            //Increment PC and move instructions to the next stage
            PC += 4;
            instruction_wb = instruction_mem;
            instruction_mem = instruction_exec;
            instruction_exec = instruction_decode;
            instruction_decode = instruction_fetch;
            instruction_fetch = PC/4;

            //Last instruction is finished
            if (instruction_fetch == instruction_decode)
                instruction_fetch = -1;
            if (instruction_fetch > instructions.Count - 1)
            {
                instruction_fetch = -1;
                PC -= 4;
            }

            //Data moving across pipeline registers
            ALU3.Text = ALU2.Text;
            ALU2.Text = ALU.ToString();
            Memory3.Text = Memory.ToString();

            //Simulation ends when the last instruction is executed
            if (instruction_wb == instructions.Count - 1 && (instruction_decode + instruction_exec + instruction_fetch + instruction_mem == -4))
            {
                NextCycle.Enabled = false;
                AutoSim.Enabled = false;
                timer1.Enabled = false;
            }

            //Location of each instruction 
            IFBox.Text = "Instruction " + instruction_fetch.ToString();
            IDBox.Text = "Instruction " + instruction_decode.ToString();
            ExBox.Text = "Instruction " + instruction_exec.ToString();
            MemBox.Text = "Instruction " + instruction_mem.ToString();
            WBBox.Text = "Instruction " + instruction_wb.ToString();
            PCNum.Text = PC.ToString();

            //Update instruction from previous stage to current stage
            for (int i = 3; i > 0; i--)
            {
                this.Controls["Inst" + i.ToString()].Text = this.Controls["Inst" + (i - 1).ToString()].Text;
            }
            Inst0.Text = (PC / 4).ToString();
            execute_cycle();
        }

        //Syntax checking
        private string error_check(string str)
        {
            int first_space = str.IndexOf(" ");
            if (first_space == -1)
                return "Invalid Instruction";
            string instr_name = str.Substring(0,first_space);
            if(!Dict.ContainsKey(instr_name))
                return "Instruction " + instr_name + " Not found";
            string format = str.Substring(first_space + 1);
                format = format.Replace(" ", "");
            for (int i = 15; i >= 0; i--)
            {
                format = format.Replace("$" + i.ToString(),"R");
            }
            for (int i = 0; i < format.Length; i++)
            {
                if (i > 0 && Char.IsDigit(format[i]) && Char.IsDigit(format[i - 1]))
                {
                    format = format.Remove(i, 1);
                    i--;
                }
            }
            for (int i = 0; i <= 9; i++)
                format = format.Replace(i.ToString(),"I");
            if (format != Dict[instr_name])
                return "Syntax error in " + instr_name + " instruction.";
            return "Right";
        }

        //Reading file, checking for errors, parsing, and loading it to the program
        private void LoadBtn_Click(object sender, EventArgs e)
        {
            StreamReader input = new StreamReader(path);
            int InstructionID = 0;
            while (!input.EndOfStream)
            {
                string tmp = input.ReadLine();
                tmp.Trim();
                if(tmp.Length > 0 && tmp[0] != '#')
                    InstStrings.Add(tmp);
            }
            PC = -4;

            //checking for errors
            foreach (string str in InstStrings)
            {
                string tmp = error_check(str);
                if (tmp != "Right")
                {
                    MessageBox.Show("Compilation Error in instruction " + str + ": " + tmp);
                    return;
                }

            }

            //Writing instructions on screen
            foreach (string str in InstStrings)
            {
                textBox1.AppendText(InstructionID.ToString() + ") " + str + "\n");
                InstructionID++;
            }
            
            //reading instructions from file and parsing
            foreach (string str in InstStrings)
            {
                instructions.Add(ParseString(str));
            }

            NextCycle.Enabled = true;
            LoadBtn.Enabled = false;
            AutoSim.Enabled = true;
        }

        //Execute next cycle and increment cycle number
        private void NextCycle_Click(object sender, EventArgs e)
        {
            next_cycle();
            CycleNum.Text = (++CycleNumber).ToString();
        }

        //Auto Simulate cycles
        private void AutoSim_Click(object sender, EventArgs e)
        {
            if (timer1.Enabled == false)
            {
                NextCycle.Enabled = false;
                timer1.Enabled = true;
                AutoSim.Text = "Stop Auto Simulation!";
            }
            else
            {
                NextCycle.Enabled = true;
                timer1.Enabled = false;
                AutoSim.Text = "Auto Simulate!";
            }
        }
       
        //Timer used for auto simulation
        private void timer1_Tick(object sender, EventArgs e)
        {
            next_cycle();
            CycleNum.Text = (++CycleNumber).ToString();
        }

        //Random number generation for memory
        private void MemRand_Click(object sender, EventArgs e)
        {
            Random rand = new Random();
            for (int i = 0; i < 16; i++)
            {
                WriteToMemory(i, rand.Next(-1000,1000));
            }
        }

        //Random number generation for registers
        private void RegRand_Click(object sender, EventArgs e)
        {
            Random rand = new Random();
            for (int i = 0; i < 16; i++)
            {
                WriteToRegister(i, rand.Next(-1000, 1000));
            }
        }

        //Reset to load a new file
        private void Reset_Click(object sender, EventArgs e)
        {
            reset_all();
            AutoSim.Enabled = false;
            NextCycle.Enabled = false;
            LoadBtn.Enabled = false;
        }
        
    }
}
