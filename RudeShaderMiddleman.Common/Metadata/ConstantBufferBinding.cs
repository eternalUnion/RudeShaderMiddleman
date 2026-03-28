using System.Collections.Generic;
using System.IO;

namespace RudeShaderMiddleman.Common.Metadata
{
	public class ConstantBufferBinding
	{
		public string Name;
		private int NameIndex;
		public int Index;
		public int ArraySize;

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
		}

		public ConstantBufferBinding(BinaryReader reader, List<string> nameMap)
		{
			NameIndex = reader.ReadInt32();
			Name = nameMap[NameIndex];
			Index = reader.ReadInt32();
			ArraySize = reader.ReadInt32();
		}
	}
}
