using Laboratory_6_Oper_sys.Helpers;
using System;
using System.Windows.Forms;

namespace Laboratory_6_Oper_sys
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        private void ButtonSelectFile_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.Cancel)
                return;

            string filename = openFileDialog.FileName;

            var compile = new CompileClass(filename);

            compile.Compile();
        }
    }
}
