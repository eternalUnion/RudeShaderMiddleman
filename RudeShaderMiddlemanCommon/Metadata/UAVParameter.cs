using System.IO;

namespace RudeShadermiddleman.Common.Metadata
{
	public class UAVParameter
	{
		public string Name;
		public int Index;
		public int OriginalIndex;

		public UAVParameter(BinaryReader reader)
		{
			Name = reader.ReadString();
			Index = reader.ReadInt32();
			OriginalIndex = reader.ReadInt32();
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Name);
			writer.Write(Index);
			writer.Write(OriginalIndex);
		}
	}
}
