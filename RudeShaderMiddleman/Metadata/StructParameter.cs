using System.Collections.Generic;
using System.IO;

namespace RudeShaderMiddleman.Metadata
{
	public class StructParameter
	{
		public string Name;
		public int Index;
		public int ArraySize;
		public int Size;
		public List<ConstantBufferParameter> CBParams;

		public StructParameter(BinaryReader reader)
		{
			Name = reader.ReadString();
			Index = reader.ReadInt32();
			ArraySize = reader.ReadInt32();
			Size = reader.ReadInt32();

			CBParams = new List<ConstantBufferParameter>();
			int cbCount = reader.ReadInt32();
			for (int i = 0; i < cbCount; i++)
			{
				CBParams.Add(new ConstantBufferParameter(reader));
			}
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Name);
			writer.Write(Index);
			writer.Write(ArraySize);
			writer.Write(Size);

			writer.Write((int)CBParams.Count);
			foreach (var param in CBParams)
			{
				param.Serialize(writer);
			}
		}
	}
}
