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
        static string path;
        
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
            
            //creating a list of instructions
            List<instruction> instructions = new List<instruction>();
            string inst;

            //reading instructions from file
            StreamReader input = new StreamReader(path);
            while (!input.EndOfStream)
            {
                inst = input.ReadLine();
                instructions.Add(ParseString(inst));
            }
            
            //testing GUI
            Reg3.Text = "5";
            Reg4.Text = "2";
            instruction x = new instruction();
            //x = new instruction(2, 3, 5, 0, 0, 0);
            WriteToRegister(x.dest_reg, ReadFromRegister(x.first_reg) - ReadFromRegister(x.second_reg));            
         }

        private void Browse_Click(object sender, EventArgs e)
        {
            //set intial directory to C:\
            openFileDialog.InitialDirectory = @"C:\";

            // Show the dialog and get result.
            DialogResult result = openFileDialog.ShowDialog();

            if (result == DialogResult.OK) // Test result.
            {
                path = openFileDialog.FileName;
                FileDir.Text = path;
            }

        }

    }
}
