using System.Collections.Generic;
using System.IO;

namespace RudeShaderMiddleman.Common.Metadata
{
	public class UAVParameter
	{
		public string Name;
		private int NameIndex;
		public int Index;
		public int OriginalIndex;

		public UAVParameter(BinaryReader reader, List<string> nameMap)
		{
			NameIndex = reader.ReadInt32();
			Name = nameMap[NameIndex];
			Index = reader.ReadInt32();
			OriginalIndex = reader.ReadInt32();
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
			writer.Write(OriginalIndex);
		}
	}
}
