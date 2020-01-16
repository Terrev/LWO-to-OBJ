using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;


namespace LRR_Models
{
	class XmlToLwo
	{
		public void ConvertFile(string inputPath, string exportPath)
		{
			exportPath = exportPath + "\\" + Path.GetFileNameWithoutExtension(inputPath) + ".lwo";

			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(File.ReadAllText(inputPath));

			FileStream fileStream = new FileStream(exportPath, FileMode.Create);
			BinaryWriter binaryWriter = new BinaryWriter2(fileStream);

			binaryWriter.Write("FORMtempLWOB".ToCharArray());

			foreach (XmlNode chunk in xmlDocument.DocumentElement.ChildNodes)
			{
				ReadChunk(chunk, fileStream, binaryWriter);
			}

			// Final length
			fileStream.Seek(4, SeekOrigin.Begin);
			binaryWriter.Write((UInt32)(fileStream.Length - 8));

			binaryWriter.Close();
			fileStream.Close();
		}

		void ReadChunk(XmlNode chunk, FileStream fileStream, BinaryWriter binaryWriter)
		{
			binaryWriter.Write(chunk.Name.ToCharArray());

			// Temp length
			binaryWriter.Write("temp".ToCharArray());
			long rememberMe = fileStream.Position;

			// Hex data check
			if (chunk.Attributes["HexData"] != null)
			{
				binaryWriter.Write(StringToByteArray(chunk.Attributes["HexData"].Value));

				// Chunk length (we're done)
				fileStream.Seek(rememberMe - 4, SeekOrigin.Begin);
				binaryWriter.Write((UInt32)(fileStream.Length - rememberMe));
				fileStream.Seek(0, SeekOrigin.End);
				return;
			}

			// NOW THE MAIN CHUNK READIN'
			if (chunk.Name == "PNTS")
			{
				foreach (XmlNode vertex in chunk.ChildNodes)
				{
					string[] splitStrings = vertex.InnerText.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (string vertValue in splitStrings)
					{
						binaryWriter.Write((float)float.Parse(vertValue));
					}
				}
			}

			else if (chunk.Name == "SRFS")
			{
				foreach (XmlNode surfaceName in chunk.ChildNodes)
				{
					binaryWriter.Write(surfaceName.InnerText.ToCharArray());
					binaryWriter.Write('\0');
					// padding
					if (surfaceName.InnerText.Length % 2 == 0)
					{
						binaryWriter.Write('\0');
					}
				}
			}

			else if (chunk.Name == "POLS")
			{
				foreach (XmlNode polygon in chunk.ChildNodes)
				{
					string[] splitStrings = polygon.Attributes["Indices"].Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					binaryWriter.Write((UInt16)splitStrings.Length); // How many verts
					foreach (string index in splitStrings)
					{
						binaryWriter.Write((UInt16)UInt16.Parse(index));
					}
					binaryWriter.Write(UInt16.Parse(polygon.Attributes["Surface"].Value));
				}
			}

			else if (chunk.Name == "SURF")
			{
				binaryWriter.Write(chunk.Attributes["SurfaceName"].Value.ToCharArray());
				binaryWriter.Write('\0');
				// padding
				if (chunk.Attributes["SurfaceName"].Value.Length % 2 == 0)
				{
					binaryWriter.Write('\0');
				}

				// sub-chunk time
				foreach (XmlNode subChunk in chunk.ChildNodes)
				{
					ReadSurfChunk(subChunk, fileStream, binaryWriter);
				}
			}

			// Chunk length
			fileStream.Seek(rememberMe - 4, SeekOrigin.Begin);
			binaryWriter.Write((UInt32)(fileStream.Length - rememberMe));
			fileStream.Seek(0, SeekOrigin.End);
		}

		void ReadSurfChunk(XmlNode chunk, FileStream fileStream, BinaryWriter binaryWriter)
		{
			binaryWriter.Write(chunk.Name.ToCharArray());

			// Temp length
			binaryWriter.Write("te".ToCharArray());
			long rememberMe = fileStream.Position;

			// Hex data check
			if (chunk.Attributes["HexData"] != null)
			{
				binaryWriter.Write(StringToByteArray(chunk.Attributes["HexData"].Value));

				// Sub-chunk length (we're done)
				fileStream.Seek(rememberMe - 2, SeekOrigin.Begin);
				binaryWriter.Write((UInt16)(fileStream.Length - rememberMe));
				fileStream.Seek(0, SeekOrigin.End);
				return;
			}

			// And on we go with the real stuff
			// Chunks that have the same structure are grouped together
			if (chunk.Name == "COLR" || chunk.Name == "TCLR")
			{
				string[] splitStrings = chunk.InnerText.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string colorValue in splitStrings)
				{
					binaryWriter.Write(byte.Parse(colorValue));
				}
				binaryWriter.Write('\0');
			}

			else if (chunk.Name == "LUMI" || chunk.Name == "DIFF" || chunk.Name == "SPEC" || chunk.Name == "GLOS" || chunk.Name == "REFL" || chunk.Name == "TRAN" || chunk.Name == "TVAL")
			{
				binaryWriter.Write(UInt16.Parse(chunk.InnerText));
			}

			else if (chunk.Name == "CTEX" || chunk.Name == "LTEX" || chunk.Name == "DTEX" || chunk.Name == "STEX" || chunk.Name == "RTEX" || chunk.Name == "TTEX" || chunk.Name == "BTEX" || chunk.Name == "TIMG" || chunk.Name == "RIMG")
			{
				binaryWriter.Write(chunk.InnerText.ToCharArray());
				binaryWriter.Write('\0');
				// padding
				if (chunk.InnerText.Length % 2 == 0)
				{
					binaryWriter.Write('\0');
				}
			}

			// Sub-chunk length
			fileStream.Seek(rememberMe - 2, SeekOrigin.Begin);
			binaryWriter.Write((UInt16)(fileStream.Length - rememberMe));
			fileStream.Seek(0, SeekOrigin.End);
		}

		public static byte[] StringToByteArray(String hex)
		{
			hex = hex.Replace(" ", "");
			int length = hex.Length;
			byte[] bytes = new byte[length / 2];
			for (int i = 0; i < length; i += 2)
			{
				bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			}
			return bytes;
		}
	}
}
