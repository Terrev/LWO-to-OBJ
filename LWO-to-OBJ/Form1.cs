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
        }

        public class Surface
        {
            public string name = "SURFACENAME";
			public string objFriendlyName = "SURFACENAME";
			public Color color = Color.White;
            public string colorTexture = "";
            public TextureFlags textureFlags = TextureFlags.None;
            public Vector3 textureSize;
            public Vector3 textureCenter;
        }

        public class Model
        {
			public string directory = "MODELDIRECTORY";
			public string name = "MODELNAME";
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
			model.objFriendlyName = RemoveSpecialCharacters(model.name);
			currentTextureType = "";

			FileStream fileStream = new FileStream(inputPath, FileMode.Open);
			BinaryReader2 binaryReader = new BinaryReader2(fileStream);

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
						Debug.WriteLine("Detail polygons found");
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
				Debug.WriteLine(surfaceName);

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
					Debug.WriteLine("Couldn't find surface named " + surfaceName);
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
					Debug.WriteLine("COLR for " + surface.name + " has a weird fourth value: " + shouldBeZero);
				}
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

				Debug.WriteLine(texturePath);

				if (texturePath != "(none)")
				{
					surface.colorTexture = texturePath;
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
				surface.textureFlags = (TextureFlags)binaryReader.ReadUInt16();
				Debug.WriteLine(surface.textureFlags);
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
				surface.textureSize = new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
				Debug.WriteLine(surface.textureSize);
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
				surface.textureCenter = new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
				Debug.WriteLine(surface.textureCenter);
			}

			else
			{
				//Debug.WriteLine("Unknown SURF sub-chunk type: " + chunkType + ", length " + chunkLength);
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
					someVector -= surface.textureCenter;
					someVector = new Vector3(someVector.X / surface.textureSize.X, someVector.Y / surface.textureSize.Y, someVector.Z / surface.textureSize.Z);

					if ((surface.textureFlags & TextureFlags.X) == TextureFlags.X)
					{
						u = 0.5f + someVector.Z;
						v = 0.5f + someVector.Y;
					}
					else if ((surface.textureFlags & TextureFlags.Y) == TextureFlags.Y)
					{
						u = 0.5f + someVector.X;
						v = 0.5f + someVector.Z;
					}
					else if ((surface.textureFlags & TextureFlags.Z) == TextureFlags.Z)
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
				model.surfaces[i].colorTexture = streamReader.ReadLine();
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
			StringBuilder sb = new StringBuilder();
			sb.Append("mtllib ").Append(model.objFriendlyName).Append(".mtl\n");
			sb.Append("\ng ").Append(model.objFriendlyName).Append("\n");

			// VERTICES
			foreach (Vector3 vertex in model.vertices)
			{
				sb.Append(string.Format("v {0} {1} {2}\n", vertex.X, vertex.Y, vertex.Z));
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
				sb.Append(string.Format("vt {0} {1}\n", uv.X, uv.Y));
			}

			// POLYGONS
			int currentSurface = model.polygons[0].surface;
			sb.Append("usemtl ").Append(model.surfaces[currentSurface - 1].objFriendlyName).Append("\n");
			foreach (Polygon polygon in model.polygons)
			{
				if (polygon.surface != currentSurface)
				{
					currentSurface = polygon.surface;
					sb.Append("usemtl ").Append(model.surfaces[currentSurface - 1].objFriendlyName).Append("\n");
				}
				sb.Append("f ");
				for (int i = 0; i < polygon.indices.Length; i++)
				{
					sb.Append(polygon.indices[i] + 1).Append("/").Append(polygon.uvIndices[i] + 1).Append(" ");
				}
				sb.Append("\n");
			}

			Directory.CreateDirectory(exportPath);
			File.WriteAllText(exportPath + "\\" + model.objFriendlyName + ".obj", sb.ToString());
			Debug.WriteLine("Saved file " + model.objFriendlyName + ".obj");

			// MTL
			StringBuilder mtlString = new StringBuilder();
			for (int i = 0; i < model.surfaces.Count; i++)
			{
				mtlString.Append("newmtl ").Append(model.surfaces[i].objFriendlyName).Append("\n");
				// Original name
				if (model.surfaces[i].name != model.surfaces[i].objFriendlyName)
				{
					mtlString.Append("# Original name: ").Append(model.surfaces[i].name).Append("\n");
				}
				// Color
				if (String.IsNullOrEmpty(model.surfaces[i].colorTexture))
				{
					Color color = model.surfaces[i].color;
					mtlString.Append("Kd ").Append((float)color.R / 255).Append(" ").Append((float)color.G / 255).Append(" ").Append((float)color.B / 255).Append("\n");
				}
				// Texture
				else
				{
					mtlString.Append("# Original texture path: ").Append(model.surfaces[i].colorTexture).Append("\n");
					string textureFileName = Path.GetFileName(model.surfaces[i].colorTexture);
					if (textureFileName.EndsWith(" (sequence)"))
					{
						textureFileName = textureFileName.Substring(0, textureFileName.Length - 11);
					}
					mtlString.Append("map_Kd ").Append(textureFileName).Append("\n");
				}
				// Gaps between surfaces
				if (i < model.surfaces.Count - 1)
				{
					mtlString.Append("\n");
				}
			}
			File.WriteAllText(exportPath + "\\" + model.objFriendlyName + ".mtl", mtlString.ToString());
			Debug.WriteLine("Saved file " + model.objFriendlyName + ".mtl");
		}
	}
}
