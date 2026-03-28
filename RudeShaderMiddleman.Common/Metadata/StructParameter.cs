using System.Collections.Generic;
using System.IO;

namespace RudeShaderMiddleman.Common.Metadata
{
	public class StructParameter
	{
		public string Name;
		private int NameIndex;
		public int Index;
		public int ArraySize;
		public int Size;
		public List<ConstantBufferParameter> CBParams;

		public StructParameter(BinaryReader reader, List<string> nameMap)
		{
			NameIndex = reader.ReadInt32();
			Name = nameMap[NameIndex];
			Index = reader.ReadInt32();
			ArraySize = reader.ReadInt32();
			Size = reader.ReadInt32();

			CBParams = new List<ConstantBufferParameter>();
			int cbCount = reader.ReadInt32();
			for (int i = 0; i < cbCount; i++)
			{
				CBParams.Add(new ConstantBufferParameter(reader, nameMap));
			}
		}

		public void Serialize(BinaryWriter writer, List<string> nameMap)
		{
			NameIndex = nameMap.IndexOf(Name);
			if (NameIndex == -1)
			{
				NameIndex = nameMap.Count;
				nameMap.Add(Name);
			}

			writer.Write(NameIndex);
			writer.Write(Index);
			writer.Write(ArraySize);
			writer.Write(Size);

			writer.Write((int)CBParams.Count);
			foreach (var param in CBParams)
			{
				param.Serialize(writer, nameMap);
			}
		}
	}
}
