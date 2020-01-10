using System;
using System.IO;

// https://stackoverflow.com/questions/8620885/c-sharp-binary-reader-in-big-endian
// lol
class BinaryReader2 : BinaryReader
{ 
	public BinaryReader2(System.IO.Stream stream) : base(stream) { }
	
	public override int ReadInt32()
	{
		var data = base.ReadBytes(4);
		Array.Reverse(data);
		return BitConverter.ToInt32(data, 0);
	}
	
	public override UInt32 ReadUInt32()
	{
		var data = base.ReadBytes(4);
		Array.Reverse(data);
		return BitConverter.ToUInt32(data, 0);
	}
	
	public override Int16 ReadInt16()
	{
		var data = base.ReadBytes(2);
		Array.Reverse(data);
		return BitConverter.ToInt16(data, 0);
	}
	
	public override UInt16 ReadUInt16()
	{
		var data = base.ReadBytes(2);
		Array.Reverse(data);
		return BitConverter.ToUInt16(data, 0);
	}
	
	public override float ReadSingle()
	{
		var data = base.ReadBytes(4);
		Array.Reverse(data);
		return BitConverter.ToSingle(data, 0);
	}
}
