using System;
using System.IO;

// https://stackoverflow.com/questions/8620885/c-sharp-binary-reader-in-big-endian
// lol again
class BinaryWriter2 : BinaryWriter
{
	public BinaryWriter2(System.IO.Stream stream) : base(stream) { }

	public override void Write(Int32 input)
	{
		byte[] data = BitConverter.GetBytes(input);
		Array.Reverse(data);
		base.Write(data);
	}

	public override void Write(UInt32 input)
	{
		byte[] data = BitConverter.GetBytes(input);
		Array.Reverse(data);
		base.Write(data);
	}

	public override void Write(Int16 input)
	{
		byte[] data = BitConverter.GetBytes(input);
		Array.Reverse(data);
		base.Write(data);
	}

	public override void Write(UInt16 input)
	{
		byte[] data = BitConverter.GetBytes(input);
		Array.Reverse(data);
		base.Write(data);
	}

	public override void Write(Single input)
	{
		byte[] data = BitConverter.GetBytes(input);
		Array.Reverse(data);
		base.Write(data);
	}
}
