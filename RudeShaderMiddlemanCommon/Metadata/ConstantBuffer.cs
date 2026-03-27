using System.Collections.Generic;
using System.IO;

namespace RudeShadermiddlemanCommon.Metadata
{
	public class ConstantBuffer
	{
		public string Name;
		public int UsedSize;
		public bool Partial;

		public List<ConstantBufferParameter> CBParams;
		public List<StructParameter> StructParams;

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Name);
			writer.Write(UsedSize);
			writer.Write(Partial);

			writer.Write((int)CBParams.Count);
			foreach (var param in CBParams)
			{
				param.Serialize(writer);
			}

			writer.Write((int)StructParams.Count);
			foreach (var param in StructParams)
			{
				param.Serialize(writer);
			}
		}

		public ConstantBuffer(BinaryReader reader)
		{
			Name = reader.ReadString();
			UsedSize = reader.ReadInt32();
			Partial = reader.ReadBoolean();

			CBParams = new List<ConstantBufferParameter>();
			int cbCount = reader.ReadInt32();
			for (int i = 0; i < cbCount; i++)
			{
				CBParams.Add(new ConstantBufferParameter(reader));
			}

			StructParams = new List<StructParameter>();
			int spCount = reader.ReadInt32();
			for (int i = 0; i < spCount; i++)
			{
				StructParams.Add(new StructParameter(reader));
			}
		}
}
}
