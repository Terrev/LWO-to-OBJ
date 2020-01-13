using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Numerics;
using System.IO;
using System.Diagnostics;

namespace LRR_Models
{
	public partial class Form1 : Form
	{
		// SURE LET'S SHOVE THIS ALL IN HERE
		public struct Polygon
		{
			public UInt16[] indices;
			public Vector2[] uv;
			public int[] uvIndices;
			public Int16 surface;
		}

		[Flags]
		public enum SurfaceFlags
		{
			None = 0,
			Luminous = 1,
			Outline = 2,
			Smoothing = 4,
			ColorHighlights = 8,
			ColorFilter = 16,
			OpaqueEdge = 32,
			TransparentEdge = 64,
			SharpTerminator = 128,
			DoubleSided = 256,
			Additive = 512,

			Unknown1024 = 1024,
			Unknown2048 = 2048,
			Unknown4096 = 4096,
			Unknown8192 = 8192,
			Unknown16384 = 16384,
			Unknown32768 = 32768,
		}

		[Flags]
		public enum TextureFlags
		{
			None = 0,
			X = 1,
			Y = 2,
			Z = 4,
			WorldCoords = 8,
			NegativeImage = 16,
			PixelBlending = 32,
			Antialiasing = 64,

			Unknown128 = 128,
			Unknown256 = 256,
			Unknown512 = 512,
			Unknown1024 = 1024,
			Unknown2048 = 2048,
			Unknown4096 = 4096,
			Unknown8192 = 8192,
			Unknown16384 = 16384,
			Unknown32768 = 32768,
		}

		public class Surface
		{
			public string name = "SURFACENAME";
			public string objFriendlyName = "SURFACENAME";
			public Color color = Color.White;
			public SurfaceFlags surfaceFlags = SurfaceFlags.None;
			public int luminosity = 0;
			public int diffuse = 0;
			public int specularity = 0;
			public int glossiness = 0;
			public int reflection = 0;
			public int transparency = 0;
			public string colorTextureImage = "";
			public TextureFlags colorTextureFlags = TextureFlags.None;
			public Vector3 colorTextureSize;
			public Vector3 colorTextureCenter;
		}

		public class Model
		{
			public string directory = "MODELDIRECTORY";
			public string name = "MODELNAME";
			public string nameWithExtension = "MODELNAMEWITHEXTENTION";
			public string objFriendlyName = "MODELNAME";
			public bool hasExternalUVs = false;
			public Vector3[] vertices;
			public List<Polygon> polygons = new List<Polygon>();
			public List<Surface> surfaces = new List<Surface>();
		}

		// UI STUFF
		public Form1()
		{
			InitializeComponent();
		}

		OpenFileDialog openFileDialog = new OpenFileDialog();

		private void checkBox1_CheckedChanged(object sender, EventArgs e)
		{
			// shrug?
		}

		private void button1_Click(object sender, EventArgs e)
		{
			openFileDialog.Filter = "LWO files (*.LWO)|*.LWO|All files (*.*)|*.*";
			openFileDialog.Multiselect = true;
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				foreach (String fileName in openFileDialog.FileNames)
				{
					LwoToObj(fileName);
				}
			}
		}

		// ACTUAL DIRTY WORK
		Model model;
		string currentTextureType = "";

		void LwoToObj(string inputPath)
		{
			model = new Model();
			model.directory = Path.GetDirectoryName(inputPath);
			model.name = Path.GetFileNameWithoutExtension(inputPath);
			model.nameWithExtension = Path.GetFileName(inputPath);
			model.objFriendlyName = RemoveSpecialCharacters(model.name);
			currentTextureType = "";

			FileStream fileStream = new FileStream(inputPath, FileMode.Open);
			BinaryReader2 binaryReader = new BinaryReader2(fileStream);

			Debug.WriteLine("\n\n==================================================================================================\nREADING FILE " + inputPath);

			// FORM
			fileStream.Seek(4, SeekOrigin.Current);

			// Amount of data after this
			binaryReader.ReadUInt32();

			// LWOB
			fileStream.Seek(4, SeekOrigin.Current);

			// Load the rest of the file
			while (fileStream.Position < fileStream.Length)
			{
				// Chunk type
				string chunkType = new string(binaryReader.ReadChars(4));

				// Chunk length
				int chunkLength = (int)binaryReader.ReadUInt32();

				ReadChunk(fileStream, binaryReader, chunkType, chunkLength);
			}

			// shrug
			binaryReader.Close();
			fileStream.Close();

			// Remove special characters
			foreach (Surface surface in model.surfaces)
			{
				surface.objFriendlyName = RemoveSpecialCharacters(surface.name);
			}

			// Flip polygons
			foreach (Polygon polygon in model.polygons)
			{
				Array.Reverse(polygon.indices);
			}

			LoadUVFile(model);

			if (!model.hasExternalUVs)
			{
				CalculateUVCoords(model);
			}

			// Flip on X for Chief
			if (checkBox1.Checked)
			{
				for (int i = 0; i < model.vertices.Length; i++)
				{
					model.vertices[i] = new Vector3(-model.vertices[i].X, model.vertices[i].Y, model.vertices[i].Z);
				}
			}

			Export(model, model.directory);
		}

		void ReadChunk(FileStream fileStream, BinaryReader binaryReader, string chunkType, int chunkLength)
		{
			if (chunkType == "PNTS")
			{
				Debug.WriteLine(chunkType + ", length " + chunkLength);
				int vertexCount = chunkLength / 12;
				model.vertices = new Vector3[vertexCount];
				for (int i = 0; i < vertexCount; i++)
				{
					model.vertices[i] = new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(), -binaryReader.ReadSingle());
				}
			}
			else if (chunkType == "SRFS")
			{
				Debug.WriteLine(chunkType + ", length " + chunkLength);

				string stuff = new string(binaryReader.ReadChars(chunkLength));
				string[] splitStrings = stuff.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < splitStrings.Length; i++)
				{
					model.surfaces.Add(new Surface());
					model.surfaces[i].name = splitStrings[i];
				}
			}
			else if (chunkType == "POLS")
			{
				Debug.WriteLine(chunkType + ", length " + chunkLength);
				long startPoint = fileStream.Position;
				while (fileStream.Position < startPoint + chunkLength)
				{
					Polygon polygon;

					UInt16 vertCount = binaryReader.ReadUInt16();
					polygon.indices = new UInt16[vertCount];
					polygon.uv = new Vector2[vertCount];
					polygon.uvIndices = new int[vertCount];
					for (int i = 0; i < vertCount; i++)
					{
						polygon.indices[i] = binaryReader.ReadUInt16();
					}
					polygon.surface = binaryReader.ReadInt16();
					model.polygons.Add(polygon);
					if (polygon.surface < 1)
					{
						Debug.WriteLine("  DETAIL POLYGONS FOUND");
					}
				}
			}
			else if (chunkType == "SURF")
			{
				Debug.WriteLine(chunkType + ", length " + chunkLength);
				long startPoint = fileStream.Position;

				List<char> chars = new List<char>();
				bool hasFoundEnd = false;
				while (!hasFoundEnd)
				{
					char currentChar = binaryReader.ReadChar();
					if (currentChar == '\0')
					{
						hasFoundEnd = true;
					}
					else
					{
						chars.Add(currentChar);
					}
				}

				string surfaceName = new string(chars.ToArray());
				Debug.WriteLine("  SURF name: " + surfaceName);

				// Padding
				if (surfaceName.Length % 2 == 0)
				{
					binaryReader.ReadByte();
				}

				Surface currentSurface = null;
				foreach (Surface surface in model.surfaces)
				{
					if (surface.name == surfaceName)
					{
						currentSurface = surface;
					}
				}
				if (currentSurface == null)
				{
					Debug.WriteLine("  Couldn't find surface named " + surfaceName);
				}

				// now check the sub-chunks
				while (fileStream.Position < startPoint + chunkLength)
				{
					// Chunk type
					string surfChunkType = new string(binaryReader.ReadChars(4));

					// Chunk length
					int surfChunkLength = (int)binaryReader.ReadUInt16();

					ReadSurfChunk(fileStream, binaryReader, surfChunkType, surfChunkLength, currentSurface);
				}
			}
			else
			{
				Debug.WriteLine("Unknown chunk type: " + chunkType + ", length " + chunkLength);
				fileStream.Seek(chunkLength, SeekOrigin.Current);
			}
		}

		void ReadSurfChunk(FileStream fileStream, BinaryReader binaryReader, string chunkType, int chunkLength, Surface surface)
		{
			if (chunkType == "COLR")
			{
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength);
				int r = (int)binaryReader.ReadByte();
				int g = (int)binaryReader.ReadByte();
				int b = (int)binaryReader.ReadByte();
				surface.color = Color.FromArgb(r, g, b);
				byte shouldBeZero = binaryReader.ReadByte();
				if (shouldBeZero != 0)
				{
					Debug.WriteLine("	  COLR for " + surface.name + " has a weird fourth value: " + shouldBeZero);
				}
			}

			else if (chunkType == "FLAG")
			{
				// SURFACE FLAGS
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength);
				surface.surfaceFlags = (SurfaceFlags)binaryReader.ReadUInt16();
				Debug.WriteLine("	  Surface Flags: " + surface.surfaceFlags);
			}

			else if (chunkType == "LUMI")
			{
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength);
				surface.luminosity = binaryReader.ReadUInt16();
				Debug.WriteLine("	  Luminosity: " + surface.luminosity);
			}
			else if (chunkType == "DIFF")
			{
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength);
				surface.diffuse = binaryReader.ReadUInt16();
				Debug.WriteLine("	  Diffuse: " + surface.diffuse);
			}
			else if (chunkType == "SPEC")
			{
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength);
				surface.specularity = binaryReader.ReadUInt16();
				Debug.WriteLine("	  Specularity: " + surface.specularity);
			}
			else if (chunkType == "GLOS")
			{
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength);
				surface.glossiness = binaryReader.ReadUInt16();
				Debug.WriteLine("	  Glossiness: " + surface.glossiness);
			}
			else if (chunkType == "REFL")
			{
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength);
				surface.reflection = binaryReader.ReadUInt16();
				Debug.WriteLine("	  Reflection: " + surface.reflection);
			}
			else if (chunkType == "TRAN")
			{
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength);
				surface.transparency = binaryReader.ReadUInt16();
				Debug.WriteLine("	  Transparency: " + surface.transparency);
			}

			else if (chunkType == "CTEX" || chunkType == "LTEX" || chunkType == "DTEX" || chunkType == "STEX" || chunkType == "RTEX" || chunkType == "TTEX" || chunkType == "BTEX")
			{
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength);
				currentTextureType = chunkType;
				fileStream.Seek(chunkLength, SeekOrigin.Current);
			}

			else if (chunkType == "TIMG")
			{
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength);
				if (currentTextureType != "CTEX")
				{
					fileStream.Seek(chunkLength, SeekOrigin.Current);
					return;
				}

				List<char> chars = new List<char>();
				bool hasFoundEnd = false;
				while (!hasFoundEnd)
				{
					char currentChar = binaryReader.ReadChar();
					if (currentChar == '\0')
					{
						hasFoundEnd = true;
					}
					else
					{
						chars.Add(currentChar);
					}
				}

				string texturePath = new string(chars.ToArray());

				// Padding
				if (texturePath.Length % 2 == 0)
				{
					binaryReader.ReadByte();
				}

				Debug.WriteLine("	  TIMG path: " + texturePath);

				if (texturePath != "(none)")
				{
					surface.colorTextureImage = texturePath;
				}
			}

			else if (chunkType == "TFLG")
			{
				// TEXTURE FLAGS
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength);
				if (currentTextureType != "CTEX")
				{
					fileStream.Seek(chunkLength, SeekOrigin.Current);
					return;
				}
				surface.colorTextureFlags = (TextureFlags)binaryReader.ReadUInt16();
				Debug.WriteLine("	  Texture Flags: " + surface.colorTextureFlags);
			}
			else if (chunkType == "TSIZ")
			{
				// TEXTURE SIZE
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength);
				if (currentTextureType != "CTEX")
				{
					fileStream.Seek(chunkLength, SeekOrigin.Current);
					return;
				}
				surface.colorTextureSize = new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
				Debug.WriteLine("	  Texture Size: " + surface.colorTextureSize);
			}
			else if (chunkType == "TCTR")
			{
				// TEXTURE CENTER
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength);
				if (currentTextureType != "CTEX")
				{
					fileStream.Seek(chunkLength, SeekOrigin.Current);
					return;
				}
				surface.colorTextureCenter = new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
				Debug.WriteLine("	  Texture Center: " + surface.colorTextureCenter);
			}

			else
			{
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength + ", UNHANDLED SURF SUB-CHUNK TYPE");
				fileStream.Seek(chunkLength, SeekOrigin.Current);
			}
		}

		public void CalculateUVCoords(Model model)
		{
			foreach (Polygon polygon in model.polygons)
			{
				Surface surface = model.surfaces[polygon.surface - 1];

				for (int i = 0; i < polygon.indices.Length; i++)
				{
					float u = 0.0f;
					float v = 0.0f;

					Vector3 someVector = model.vertices[polygon.indices[i]];
					someVector = new Vector3(someVector.X, someVector.Y, -someVector.Z);
					someVector -= surface.colorTextureCenter;
					someVector = new Vector3(someVector.X / surface.colorTextureSize.X, someVector.Y / surface.colorTextureSize.Y, someVector.Z / surface.colorTextureSize.Z);

					if ((surface.colorTextureFlags & TextureFlags.X) == TextureFlags.X)
					{
						u = 0.5f + someVector.Z;
						v = 0.5f + someVector.Y;
					}
					else if ((surface.colorTextureFlags & TextureFlags.Y) == TextureFlags.Y)
					{
						u = 0.5f + someVector.X;
						v = 0.5f + someVector.Z;
					}
					else if ((surface.colorTextureFlags & TextureFlags.Z) == TextureFlags.Z)
					{
						u = 0.5f + someVector.X;
						v = 0.5f + someVector.Y;
					}

					polygon.uv[i] = new Vector2(u, v);
				}
			}
		}

		public void LoadUVFile(Model model)
		{
			string uvFilePath = model.directory + "\\" + model.name + ".uv";

			if (!File.Exists(uvFilePath))
			{
				return;
			}

			model.hasExternalUVs = true;

			StreamReader streamReader = new StreamReader(uvFilePath);

			// Always 2, maybe a format version?
			streamReader.ReadLine();

			int surfaceCount = int.Parse(streamReader.ReadLine());

			// Surface names
			for (int i = 0; i < surfaceCount; i++)
			{
				streamReader.ReadLine();
			}

			// Surface texture paths
			for (int i = 0; i < surfaceCount; i++)
			{
				model.surfaces[i].colorTextureImage = streamReader.ReadLine();
			}

			int polyCount = int.Parse(streamReader.ReadLine());

			// UVs
			for (int i = 0; i < polyCount; i++)
			{
				// Number of the poly we're on (unneeded) and the amount of verts it has
				string polyInfo = streamReader.ReadLine();
				string[] splitPolyInfo = polyInfo.Split(' ');
				int numberOfVerts = int.Parse(splitPolyInfo[1]);
				Vector2[] parsedCoords = new Vector2[numberOfVerts];
				for (int j = 0; j < numberOfVerts; j++)
				{
					string uvCoords = streamReader.ReadLine();
					string[] splitCoords = uvCoords.Split(' ');
					parsedCoords[j] = new Vector2(float.Parse(splitCoords[0]), -float.Parse(splitCoords[1]) + 1.0f);
				}
				Array.Reverse(parsedCoords);
				for (int j = 0; j < numberOfVerts; j++)
				{
					model.polygons[i].uv[j] = parsedCoords[j];
				}
			}

			streamReader.Close();
		}

		string RemoveSpecialCharacters(string str)
		{
			StringBuilder sb = new StringBuilder();
			foreach (char c in str)
			{
				if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_')
				{
					sb.Append(c);
				}
				else if (c == ' ' || c == '-')
				{
					sb.Append("_");
				}
			}
			return sb.ToString();
		}

		public void Export(Model model, string exportPath)
		{
			StringBuilder objString = new StringBuilder();
			objString.Append("# ").Append(model.nameWithExtension).Append("\n\n");
			objString.Append("mtllib ").Append(model.objFriendlyName).Append(".mtl\n");
			objString.Append("\ng ").Append(model.objFriendlyName).Append("\n");

			// VERTICES
			foreach (Vector3 vertex in model.vertices)
			{
				objString.Append(string.Format("v {0} {1} {2}\n", vertex.X, vertex.Y, vertex.Z));
			}

			// UV
			// generate list of existing coords
			List<Vector2> existingUVCoords = new List<Vector2>();
			foreach (Polygon polygon in model.polygons)
			{
				for (int i = 0; i < polygon.uv.Length; i++)
				{
					int findResults = existingUVCoords.IndexOf(polygon.uv[i]);
					if (findResults == -1)
					{
						existingUVCoords.Add(polygon.uv[i]);
						polygon.uvIndices[i] = existingUVCoords.Count - 1;
					}
					else
					{
						polygon.uvIndices[i] = findResults;
					}
				}
			}
			// write list
			foreach (Vector2 uv in existingUVCoords)
			{
				objString.Append(string.Format("vt {0} {1}\n", uv.X, uv.Y));
			}

			// POLYGONS
			int currentSurface = model.polygons[0].surface;
			objString.Append("usemtl ").Append(model.surfaces[currentSurface - 1].objFriendlyName).Append("\n");
			foreach (Polygon polygon in model.polygons)
			{
				if (polygon.surface != currentSurface)
				{
					currentSurface = polygon.surface;
					objString.Append("usemtl ").Append(model.surfaces[currentSurface - 1].objFriendlyName).Append("\n");
				}
				objString.Append("f ");
				for (int i = 0; i < polygon.indices.Length; i++)
				{
					objString.Append(polygon.indices[i] + 1).Append("/").Append(polygon.uvIndices[i] + 1).Append(" ");
				}
				objString.Append("\n");
			}

			Directory.CreateDirectory(exportPath);
			File.WriteAllText(exportPath + "\\" + model.objFriendlyName + ".obj", objString.ToString());
			Debug.WriteLine("Saved file " + model.objFriendlyName + ".obj");

			// MTL
			StringBuilder mtlString = new StringBuilder();
			mtlString.Append("# ").Append(model.nameWithExtension).Append("\n\n");
			for (int i = 0; i < model.surfaces.Count; i++)
			{
				mtlString.Append("# Surface name:  ").Append(model.surfaces[i].name).Append("\n");
				mtlString.Append("# Surface flags: ").Append(model.surfaces[i].surfaceFlags).Append("\n");

				mtlString.Append("# Color (RGB):   ").Append(model.surfaces[i].color.R + " " + model.surfaces[i].color.G + " " + model.surfaces[i].color.B).Append("\n");

				// lol
				mtlString.Append("# Luminosity:    ").Append(model.surfaces[i].luminosity).Append(" (").Append(((float)model.surfaces[i].luminosity / 256) * 100.0f).Append("%)").Append("\n");
				mtlString.Append("# Diffuse:       ").Append(model.surfaces[i].diffuse).Append(" (").Append(((float)model.surfaces[i].diffuse / 256) * 100.0f).Append("%)").Append("\n");
				mtlString.Append("# Specularity:   ").Append(model.surfaces[i].specularity).Append(" (").Append(((float)model.surfaces[i].specularity / 256) * 100.0f).Append("%)").Append("\n");
				mtlString.Append("# Glossiness     ").Append(model.surfaces[i].glossiness).Append(" (").Append(((float)model.surfaces[i].glossiness / 256) * 100.0f).Append("%)").Append("\n");
				mtlString.Append("# Reflection:    ").Append(model.surfaces[i].reflection).Append(" (").Append(((float)model.surfaces[i].reflection / 256) * 100.0f).Append("%)").Append("\n");
				mtlString.Append("# Transparency:  ").Append(model.surfaces[i].transparency).Append(" (").Append(((float)model.surfaces[i].transparency / 256) * 100.0f).Append("%)").Append("\n");

				mtlString.Append("# Texture flags: ").Append(model.surfaces[i].colorTextureFlags).Append("\n");
				mtlString.Append("# Texture path:  ").Append(model.surfaces[i].colorTextureImage).Append("\n");

				mtlString.Append("newmtl ").Append(model.surfaces[i].objFriendlyName).Append("\n");
				// Color
				if (String.IsNullOrEmpty(model.surfaces[i].colorTextureImage))
				{
					Color color = model.surfaces[i].color;
					mtlString.Append("Kd ").Append((float)color.R / 255).Append(" ").Append((float)color.G / 255).Append(" ").Append((float)color.B / 255).Append("\n");
				}
				// Texture
				else
				{
					string textureFileName = Path.GetFileName(model.surfaces[i].colorTextureImage);
					if (textureFileName.EndsWith(" (sequence)"))
					{
						textureFileName = textureFileName.Substring(0, textureFileName.Length - 11);
					}
					mtlString.Append("map_Kd ").Append(textureFileName).Append("\n");
				}
				// Empty lines between surfaces
				if (i < model.surfaces.Count - 1)
				{
					mtlString.Append("\n");
				}
			}
			File.WriteAllText(exportPath + "\\" + model.objFriendlyName + ".mtl", mtlString.ToString());
			Debug.WriteLine("Saved file " + model.objFriendlyName + ".mtl");

			Debug.WriteLine("==================================================================================================\n\n");
		}
	}
}
