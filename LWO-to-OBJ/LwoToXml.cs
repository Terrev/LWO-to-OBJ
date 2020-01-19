using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Diagnostics;

namespace LRR_Models
{
	class LwoToXml
	{
		public void ConvertFile(string inputPath, string exportPath)
		{
			Debug.WriteLine("\n\n==================================================================================================\nREADING FILE " + inputPath);

			FileStream fileStream = new FileStream(inputPath, FileMode.Open);
			BinaryReader2 binaryReader = new BinaryReader2(fileStream);

			XmlDocument xmlDocument = new XmlDocument();

			XmlElement root = xmlDocument.CreateElement("LWO");
			xmlDocument.AppendChild(root);

			// FORM
			fileStream.Seek(4, SeekOrigin.Current);

			// Amount of data after this
			binaryReader.ReadUInt32();

			// LWOB
			fileStream.Seek(4, SeekOrigin.Current);

			// Load the rest of the file
			while (fileStream.Position < fileStream.Length)
			{
				ReadChunk(fileStream, binaryReader, xmlDocument);
			}

			binaryReader.Close();
			fileStream.Close();

			string exportPathWithFileName = exportPath + "\\" + Path.GetFileNameWithoutExtension(inputPath) + ".xml";

			XmlWriterSettings settings = new XmlWriterSettings
			{
				Indent = true,
				IndentChars = "	",
				NewLineOnAttributes = false

			};
			using (XmlWriter writer = XmlWriter.Create(exportPathWithFileName, settings))
			{
				xmlDocument.Save(writer);
			}
			Debug.WriteLine("Saved file " + exportPathWithFileName);
			Debug.WriteLine("==================================================================================================\n\n");
		}

		void ReadChunk(FileStream fileStream, BinaryReader binaryReader, XmlDocument xmlDocument)
		{
			string chunkType = new string(binaryReader.ReadChars(4));

			int chunkLength = (int)binaryReader.ReadUInt32();

			XmlElement chunk = xmlDocument.CreateElement(chunkType);
			xmlDocument.DocumentElement.AppendChild(chunk);

			if (chunkType == "PNTS")
			{
				Debug.WriteLine(chunkType + ", length " + chunkLength);
				if (Program.floats1)
				{
					int vertexCount = chunkLength / 12;
					for (int i = 0; i < vertexCount; i++)
					{
						XmlElement vertex = xmlDocument.CreateElement("Vertex");
						vertex.InnerText = binaryReader.ReadSingle() + "," + binaryReader.ReadSingle() + "," + binaryReader.ReadSingle();
						chunk.AppendChild(vertex);
					}
				}
				else
				{
					byte[] byteArray = binaryReader.ReadBytes(chunkLength);
					string hex = BitConverter.ToString(byteArray).Replace("-", " ");
					chunk.SetAttribute("HexData", hex);
				}
			}

			else if (chunkType == "SRFS")
			{
				Debug.WriteLine(chunkType + ", length " + chunkLength);
				string stuff = new string(binaryReader.ReadChars(chunkLength));
				string[] splitStrings = stuff.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < splitStrings.Length; i++)
				{
					XmlElement surface = xmlDocument.CreateElement("SurfaceName");
					surface.InnerText = splitStrings[i];
					chunk.AppendChild(surface);
				}
			}

			else if (chunkType == "POLS")
			{
				Debug.WriteLine(chunkType + ", length " + chunkLength);
				long startPoint = fileStream.Position;
				while (fileStream.Position < startPoint + chunkLength)
				{
					XmlElement polygon = xmlDocument.CreateElement("Polygon");
					chunk.AppendChild(polygon);
					UInt16 vertCount = binaryReader.ReadUInt16();
					UInt16[] indices = new UInt16[vertCount];
					for (int i = 0; i < vertCount; i++)
					{
						indices[i] = binaryReader.ReadUInt16();
					}
					polygon.SetAttribute("Indices", string.Join(",", indices));
					Int16 surface = binaryReader.ReadInt16();
					polygon.SetAttribute("Surface", surface.ToString());

					if (surface < 1)
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
				chunk.SetAttribute("SurfaceName", surfaceName);
				// Padding
				if (surfaceName.Length % 2 == 0)
				{
					binaryReader.ReadByte();
				}

				// now check the sub-chunks
				while (fileStream.Position < startPoint + chunkLength)
				{
					ReadSurfChunk(fileStream, binaryReader, xmlDocument, chunk);
				}
			}

			else
			{
				Debug.WriteLine(chunkType + ", length " + chunkLength);
				byte[] byteArray = binaryReader.ReadBytes(chunkLength);
				string hex = BitConverter.ToString(byteArray).Replace("-", " ");
				chunk.SetAttribute("HexData", hex);
			}
		}

		void ReadSurfChunk(FileStream fileStream, BinaryReader binaryReader, XmlDocument xmlDocument, XmlElement parentChunk)
		{
			string chunkType = new string(binaryReader.ReadChars(4));

			int chunkLength = (int)binaryReader.ReadUInt16();

			XmlElement chunk = xmlDocument.CreateElement(chunkType);
			parentChunk.AppendChild(chunk);

			if (chunkType == "COLR" || chunkType == "TCLR")
			{
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength);
				int r = (int)binaryReader.ReadByte();
				int g = (int)binaryReader.ReadByte();
				int b = (int)binaryReader.ReadByte();
				byte supposedlyUnused = binaryReader.ReadByte();
				chunk.SetAttribute("Color", r + "," + g + "," + b);
				chunk.SetAttribute("SupposedlyUnused", supposedlyUnused.ToString("X2"));
			}

			else if (chunkType == "FLAG")
			{
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength);
				char[] dumb = Convert.ToString(binaryReader.ReadUInt16(), 2).PadLeft(16, '0').ToCharArray();
				Array.Reverse(dumb);

				string[] flagNames =
				{
					"Luminous",
					"Outline",
					"Smoothing",
					"ColorHighlights",
					"ColorFilter",
					"OpaqueEdge",
					"TransparentEdge",
					"SharpTerminator",
					"DoubleSided",
					"Additive",
					"Unknown1024",
					"Unknown2048",
					"Unknown4096",
					"Unknown8192",
					"Unknown16384",
					"Unknown32768"
				};

				for (int i = 0; i < 16; i++)
				{
					XmlElement flag = xmlDocument.CreateElement(flagNames[i]);
					chunk.AppendChild(flag);
					flag.InnerText = dumb[i].ToString();
				}
			}

			else if (chunkType == "LUMI" || chunkType == "DIFF" || chunkType == "SPEC" || chunkType == "REFL" || chunkType == "TRAN" || chunkType == "GLOS" || chunkType == "TVAL")
			{
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength);
				chunk.InnerText = binaryReader.ReadUInt16().ToString();
			}

			else if (chunkType == "VLUM" || chunkType == "VDIF" || chunkType == "VSPC" || chunkType == "VRFL" || chunkType == "VTRN" || chunkType == "TAMP" || chunkType == "TAAS" || chunkType == "TOPC")
			{
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength);
				if (Program.floats2)
				{
					chunk.InnerText = binaryReader.ReadSingle().ToString();
				}
				else
				{
					byte[] byteArray = binaryReader.ReadBytes(chunkLength);
					string hex = BitConverter.ToString(byteArray).Replace("-", " ");
					chunk.SetAttribute("HexData", hex);
				}
			}

			else if (chunkType == "RSAN" || chunkType == "SMAN")
			{
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength);
				if (Program.floats3)
				{
					chunk.InnerText = binaryReader.ReadSingle().ToString();
				}
				else
				{
					byte[] byteArray = binaryReader.ReadBytes(chunkLength);
					string hex = BitConverter.ToString(byteArray).Replace("-", " ");
					chunk.SetAttribute("HexData", hex);
				}
			}

			else if (chunkType == "CTEX" || chunkType == "LTEX" || chunkType == "DTEX" || chunkType == "STEX" || chunkType == "RTEX" || chunkType == "TTEX" || chunkType == "BTEX" || chunkType == "TIMG" || chunkType == "RIMG")
			{
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength);

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

				chunk.InnerText = texturePath;

				Debug.WriteLine("	  " + chunkType + " path: " + texturePath);
			}

			else if (chunkType == "TFLG")
			{
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength);
				char[] dumb = Convert.ToString(binaryReader.ReadUInt16(), 2).PadLeft(16, '0').ToCharArray();
				Array.Reverse(dumb);

				string[] flagNames =
				{
					"X",
					"Y",
					"Z",
					"WorldCoords",
					"NegativeImage",
					"PixelBlending",
					"Antialiasing",
					"Unknown128",
					"Unknown256",
					"Unknown512",
					"Unknown1024",
					"Unknown2048",
					"Unknown4096",
					"Unknown8192",
					"Unknown16384",
					"Unknown32768"
				};

				for (int i = 0; i < 16; i++)
				{
					XmlElement flag = xmlDocument.CreateElement(flagNames[i]);
					chunk.AppendChild(flag);
					flag.InnerText = dumb[i].ToString();
				}
			}

			else if (chunkType == "TSIZ" || chunkType == "TCTR" || chunkType == "TFAL" || chunkType == "TVEL")
			{
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength);
				if (Program.floats4)
				{
					float x = binaryReader.ReadSingle();
					float y = binaryReader.ReadSingle();
					float z = binaryReader.ReadSingle();
					chunk.InnerText = x + "," + y + "," + z;
				}
				else
				{
					byte[] byteArray = binaryReader.ReadBytes(chunkLength);
					string hex = BitConverter.ToString(byteArray).Replace("-", " ");
					chunk.SetAttribute("HexData", hex);
				}
			}

			else
			{
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength + ", UNHANDLED SURF SUB-CHUNK TYPE");
				byte[] byteArray = binaryReader.ReadBytes(chunkLength);
				string hex = BitConverter.ToString(byteArray).Replace("-", " ");
				chunk.SetAttribute("HexData", hex);
			}
		}
	}
}
