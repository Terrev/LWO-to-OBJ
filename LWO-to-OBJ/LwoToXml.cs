using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.IO;
using System.Xml;
using System.Diagnostics;

namespace LRR_Models
{
	class LwoToXml
	{
		public void ConvertFile(string inputPath, string exportPath)
		{
			exportPath = exportPath + "\\" + Path.GetFileNameWithoutExtension(inputPath) + "_test.xml";

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
				// Chunk type
				string chunkType = new string(binaryReader.ReadChars(4));

				// Chunk length
				int chunkLength = (int)binaryReader.ReadUInt32();

				ReadChunk(fileStream, binaryReader, chunkType, chunkLength, xmlDocument);
			}

			binaryReader.Close();
			fileStream.Close();

			XmlWriterSettings settings = new XmlWriterSettings
			{
				Indent = true,
				IndentChars = "	",
				NewLineOnAttributes = false

			};
			using (XmlWriter writer = XmlWriter.Create(exportPath, settings))
			{
				xmlDocument.Save(writer);
			}
		}

		void ReadChunk(FileStream fileStream, BinaryReader binaryReader, string chunkType, int chunkLength, XmlDocument xmlDocument)
		{
			XmlElement chunk = xmlDocument.CreateElement(chunkType);
			xmlDocument.DocumentElement.AppendChild(chunk);

			if (chunkType == "PNTS")
			{
				if (Program.writeFloatingPointToText)
				{
					Debug.WriteLine(chunkType + ", length " + chunkLength);
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
					Debug.WriteLine(chunkType + ", length " + chunkLength);
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
					// Chunk type
					string surfChunkType = new string(binaryReader.ReadChars(4));

					// Chunk length
					int surfChunkLength = (int)binaryReader.ReadUInt16();

					ReadSurfChunk(fileStream, binaryReader, surfChunkType, surfChunkLength, xmlDocument, chunk);
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

		void ReadSurfChunk(FileStream fileStream, BinaryReader binaryReader, string chunkType, int chunkLength, XmlDocument xmlDocument, XmlElement parentChunk)
		{
			XmlElement chunk = xmlDocument.CreateElement(chunkType);
			parentChunk.AppendChild(chunk);

			if (chunkType == "COLR")
			{
				Debug.WriteLine("	" + chunkType + ", length " + chunkLength);
				int r = (int)binaryReader.ReadByte();
				int g = (int)binaryReader.ReadByte();
				int b = (int)binaryReader.ReadByte();
				byte shouldBeZero = binaryReader.ReadByte();
				if (shouldBeZero != 0)
				{
					Debug.WriteLine("	  COLR has a weird fourth value: " + shouldBeZero);
				}
				chunk.InnerText = r + "," + g + "," + b;
			}

			else if (chunkType == "TIMG")
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

				Debug.WriteLine("	  TIMG path: " + texturePath);
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
