using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace LRR_Models
{
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
}
