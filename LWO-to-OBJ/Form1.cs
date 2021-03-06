﻿using System;
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

		// LWO to OBJ
		OpenFileDialog openFileDialog1 = new OpenFileDialog();
		VistaFolderBrowserDialog folderBrowserDialog1 = new Ookii.Dialogs.VistaFolderBrowserDialog();

		// LWO to XML
		OpenFileDialog openFileDialog2 = new OpenFileDialog();
		VistaFolderBrowserDialog folderBrowserDialog2 = new Ookii.Dialogs.VistaFolderBrowserDialog();

		// XML to LWO
		OpenFileDialog openFileDialog3 = new OpenFileDialog();
		VistaFolderBrowserDialog folderBrowserDialog3 = new Ookii.Dialogs.VistaFolderBrowserDialog();

		private void button1_Click(object sender, EventArgs e)
		{
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
						lwoToObj.ConvertFile(fileName, folderBrowserDialog1.SelectedPath);
					}

					Form2 form2 = new Form2();
					form2.ShowDialog();
				}
			}
		}

		private void button2_Click(object sender, EventArgs e)
		{
			Program.floats1 = checkBox2.Checked;
			Program.floats2 = checkBox3.Checked;
			Program.floats3 = checkBox4.Checked;
			Program.floats4 = checkBox5.Checked;

			LwoToXml lwoToXml = new LwoToXml();

			openFileDialog2.Filter = "LWO files (*.LWO)|*.LWO|All files (*.*)|*.*";
			openFileDialog2.Multiselect = true;
			openFileDialog2.Title = "Select LWO file(s) to convert";

			folderBrowserDialog2.Description = "Select location to save XML file(s)";
			folderBrowserDialog2.UseDescriptionForTitle = true;

			if (openFileDialog2.ShowDialog() == DialogResult.OK)
			{
				if (folderBrowserDialog2.ShowDialog() == DialogResult.OK)
				{
					foreach (String fileName in openFileDialog2.FileNames)
					{
						lwoToXml.ConvertFile(fileName, folderBrowserDialog2.SelectedPath);
					}

					Form2 form2 = new Form2();
					form2.ShowDialog();
				}
			}
		}

		private void button3_Click(object sender, EventArgs e)
		{
			XmlToLwo xmlToLwo = new XmlToLwo();

			openFileDialog3.Filter = "XML files (*.XML)|*.XML|All files (*.*)|*.*";
			openFileDialog3.Multiselect = true;
			openFileDialog3.Title = "Select XML file(s) to convert";

			folderBrowserDialog3.Description = "Select location to save LWO file(s)";
			folderBrowserDialog3.UseDescriptionForTitle = true;

			if (openFileDialog3.ShowDialog() == DialogResult.OK)
			{
				if (folderBrowserDialog3.ShowDialog() == DialogResult.OK)
				{
					foreach (String fileName in openFileDialog3.FileNames)
					{
						xmlToLwo.ConvertFile(fileName, folderBrowserDialog3.SelectedPath);
					}

					Form2 form2 = new Form2();
					form2.ShowDialog();
				}
			}
		}

		private void Form1_Load(object sender, EventArgs e)
		{

		}
	}
}
