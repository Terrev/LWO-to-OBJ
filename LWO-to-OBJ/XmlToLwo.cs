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
                binaryWriter.Write(chunk.Name.ToCharArray());

                // Temp length
                binaryWriter.Write("temp".ToCharArray());
                long rememberMe = fileStream.Position;

                if (chunk.Name == "SRFS")
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
                        string indicesString = polygon.Attributes["Indices"].Value;
                        string[] splitStrings = indicesString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        binaryWriter.Write((UInt16)splitStrings.Length);
                        foreach (string help in splitStrings)
                        {
                            binaryWriter.Write((UInt16)UInt16.Parse(help));
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

                    // sub-chunks
                    foreach (XmlNode subChunk in chunk.ChildNodes)
                    {
                        // haaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa

                        binaryWriter.Write(subChunk.Name.ToCharArray());

                        // Temp length
                        binaryWriter.Write("te".ToCharArray());
                        long rememberMeAgain = fileStream.Position;

                        if (subChunk.Name == "COLR")
                        {
                            string colorString = subChunk.InnerText;
                            string[] splitStrings = colorString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string asdfsdhj in splitStrings)
                            {
                                binaryWriter.Write(byte.Parse(asdfsdhj));
                            }
                            binaryWriter.Write('\0');
                        }

                        else
                        {
                            binaryWriter.Write(StringToByteArray(subChunk.Attributes["HexData"].Value));
                        }

                        // Sub-chunk length
                        fileStream.Seek(rememberMeAgain - 2, SeekOrigin.Begin);
                        long lengthAsdf = fileStream.Length - rememberMeAgain;
                        binaryWriter.Write((UInt16)lengthAsdf);
                        fileStream.Seek(0, SeekOrigin.End);
                    }
                }

                else
                {
                    binaryWriter.Write(StringToByteArray(chunk.Attributes["HexData"].Value));
                }

                // Chunk length
                fileStream.Seek(rememberMe - 4, SeekOrigin.Begin);
                long length = fileStream.Length - rememberMe;
                binaryWriter.Write((UInt32)length);
                fileStream.Seek(0, SeekOrigin.End);
            }

            // Final length
            fileStream.Seek(4, SeekOrigin.Begin);
            binaryWriter.Write((UInt32)fileStream.Length - 8);

            binaryWriter.Close();
            fileStream.Close();
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
