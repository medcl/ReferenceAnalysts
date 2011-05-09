using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Glee;
using Microsoft.Glee.Drawing;
using Microsoft.Glee.GraphViewerGdi;
using Microsoft.Glee.Splines;
using Color = System.Drawing.Color;
using Node = Microsoft.Glee.Drawing.Node;

namespace Dependency
{
	public partial class Form1 : Form
	{
		private readonly GViewer _viewer;

		private HashSet<string> _references = new HashSet<string>();
		private string folder;

		public Form1()
		{
			InitializeComponent();
			_viewer = new GViewer();
			_viewer.BorderStyle = BorderStyle.None;

			_viewer.PanButtonPressed = true;

			_viewer.MouseClick += viewer_MouseClick;
			_viewer.MouseDoubleClick += viewer_MouseDoubleClick;
			_viewer.MouseWheel += viewer_MouseWheel;
			_viewer.OutsideAreaBrush = new Pen(Color.White).Brush;
			_viewer.Dock = DockStyle.Fill;
			_viewer.AutoScaleMode = AutoScaleMode.Dpi;
			panel1.Controls.Add(_viewer);
		}

		private void button2_Click(object sender, EventArgs e)
		{
			if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
			{
				folder = folderBrowserDialog1.SelectedPath;
				_references = new HashSet<string>();
				comboBox1.Items.Clear();
				string[] directory = Directory.GetFiles(folder);
				foreach (string assemblyFile in directory)
				{
					if (assemblyFile.ToLower().EndsWith(".dll"))
					{
						try
						{
							Assembly assembly = Assembly.ReflectionOnlyLoadFrom(assemblyFile);
							AssemblyName[] references = assembly.GetReferencedAssemblies();
							string filename = Path.GetFileName(assemblyFile);
							foreach (AssemblyName file in references)
							{
								if (file.Name.ToLower().StartsWith("system") || file.Name.ToLower().StartsWith("mscorlib"))
								{
									continue;
								}
								_references.Add(string.Format("{0},{1}.dll", filename, file.Name));
							}
							comboBox1.Items.Add(filename);
						}
						catch (BadImageFormatException)
						{
							continue;
						}
						catch(FileLoadException)
						{
							continue;
						}catch(Exception)
						{
							Debugger.Break();
						}
					}
				}
			}
		}


		private void viewer_MouseClick(object sender, MouseEventArgs e)
		{
		}


		private object GetObjectAt(int x, int y)
		{
			object result = null;
			try
			{
				result = _viewer.GetObjectAt(x, y);
			}
			catch (NullReferenceException)
			{
			}
			return result;
		}

		private void viewer_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			var node = GetObjectAt(e.X, e.Y) as Node;

			if (node != null) Analyze(node.Id);
			//more thing here....
		}

		private void viewer_MouseWheel(object sender, MouseEventArgs e)
		{
			if (e.Delta < 0)
				_viewer.ZoomF *= 1.1;
			else
				_viewer.ZoomF *= 0.9;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			string dll = comboBox1.Text;
			Analyze(dll);
		}

		private void Analyze(string dll)
		{
			if (!string.IsNullOrEmpty(dll))
			{
				toolStripStatusLabel1.Text = dll;
				var graph = new Graph("glee");
				richTextBox1.Clear();
				richTextBox2.Clear();

				foreach (string relation in _references)
				{
					if (relation.StartsWith(dll))
					{
						string[] strs = relation.Split(',');
						graph.AddEdge(strs[0], strs[1]).Attr.Color=Microsoft.Glee.Drawing.Color.DarkBlue;
						richTextBox1.AppendText(strs[1] + "\n");
						graph.FindNode(strs[1]).Attr.Fillcolor = Microsoft.Glee.Drawing.Color.Blue;
					}else if(relation.EndsWith(dll))
					{
						string[] strs = relation.Split(',');
						graph.AddEdge(strs[0], strs[1]).Attr.Color = Microsoft.Glee.Drawing.Color.DarkRed; 
						richTextBox2.AppendText(strs[0] + "\n");
						graph.FindNode(strs[0]).Attr.Fillcolor = Microsoft.Glee.Drawing.Color.Red;
					}
				}
				var node=graph.FindNode(dll);
				if(node !=null)node.Attr.Fillcolor = Microsoft.Glee.Drawing.Color.Green;
				_viewer.Graph = graph;
				_viewer.CalculateLayout(graph);
			}
		}
	}
}