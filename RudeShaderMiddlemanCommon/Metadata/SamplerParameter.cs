using System.IO;

namespace RudeShadermiddleman.Common.Metadata
{
	public class SamplerParameter
	{
		public uint Sampler;
		public int BindPoint;

		public SamplerParameter(BinaryReader reader)
		{
			Sampler = reader.ReadUInt32();
			BindPoint = reader.ReadInt32();
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Sampler);
			writer.Write(BindPoint);
		}
	}
}
