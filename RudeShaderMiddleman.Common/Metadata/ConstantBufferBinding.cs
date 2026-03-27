using System.IO;

namespace RudeShaderMiddleman.Common.Metadata
{
	public class ConstantBufferBinding
	{
		public string Name;
		public int Index;
		public int ArraySize;

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Name);
			writer.Write(Index);
			writer.Write(ArraySize);
		}

		public ConstantBufferBinding(BinaryReader reader)
		{
			Name = reader.ReadString();
			Index = reader.ReadInt32();
			ArraySize = reader.ReadInt32();
		}
	}
}
