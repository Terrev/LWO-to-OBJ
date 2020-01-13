using System;
using System.Windows.Forms;
using Ookii.Dialogs;

namespace LRR_Models
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		OpenFileDialog openFileDialog1 = new OpenFileDialog();
		VistaFolderBrowserDialog folderBrowserDialog1 = new Ookii.Dialogs.VistaFolderBrowserDialog();

		private void checkBox1_CheckedChanged(object sender, EventArgs e)
		{
			Program.flipOnX = checkBox1.Checked;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			// shrug, why not
			Program.flipOnX = checkBox1.Checked;

			LwoToObj lwoToObj = new LwoToObj();

			openFileDialog1.Filter = "LWO files (*.LWO)|*.LWO|All files (*.*)|*.*";
			openFileDialog1.Multiselect = true;

			openFileDialog1.Title = "Select LWO file(s) to convert";
			folderBrowserDialog1.Description = "Select location to save OBJ/MTL files";
			folderBrowserDialog1.UseDescriptionForTitle = true;

			if (openFileDialog1.ShowDialog() == DialogResult.OK)
			{
				if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
				{
					foreach (String fileName in openFileDialog1.FileNames)
					{
						lwoToObj.Convert(fileName, folderBrowserDialog1.SelectedPath);
					}

					Form2 form2 = new Form2();
					form2.ShowDialog();
				}
			}
		}
	}
}
