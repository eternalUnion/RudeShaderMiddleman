using System.Collections.Generic;
using System.IO;

namespace RudeShaderMiddleman.Common.Metadata
{
	public class ConstantBuffer
	{
		public string Name;
		private int NameIndex;
		public int UsedSize;
		public bool Partial;

		public List<ConstantBufferParameter> CBParams;
		public List<StructParameter> StructParams;

		public void Serialize(BinaryWriter writer, List<string> nameMap)
		{
			NameIndex = nameMap.IndexOf(Name);
			if (NameIndex == -1)
			{
				NameIndex = nameMap.Count;
				nameMap.Add(Name);
			}

			writer.Write(NameIndex);
			writer.Write(UsedSize);
			writer.Write(Partial);

			writer.Write((int)CBParams.Count);
			foreach (var param in CBParams)
			{
				param.Serialize(writer, nameMap);
			}

			writer.Write((int)StructParams.Count);
			foreach (var param in StructParams)
			{
				param.Serialize(writer, nameMap);
			}
		}

		public ConstantBuffer(BinaryReader reader, List<string> nameMap)
		{
			NameIndex = reader.ReadInt32();
			Name = nameMap[NameIndex];
			UsedSize = reader.ReadInt32();
			Partial = reader.ReadBoolean();

			CBParams = new List<ConstantBufferParameter>();
			int cbCount = reader.ReadInt32();
			for (int i = 0; i < cbCount; i++)
			{
				CBParams.Add(new ConstantBufferParameter(reader, nameMap));
			}

			StructParams = new List<StructParameter>();
			int spCount = reader.ReadInt32();
			for (int i = 0; i < spCount; i++)
			{
				StructParams.Add(new StructParameter(reader, nameMap));
			}
		}
	}
}
