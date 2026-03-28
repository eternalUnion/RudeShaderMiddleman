using System.Collections.Generic;
using System.IO;

namespace RudeShaderMiddleman.Common.Metadata
{
    public class TextureParameter
    {
        public string Name;
        private int NameIndex;
        public int Index;
        public int SamplerIndex;
        public bool MultiSampled;
        public byte Dim;

		public TextureParameter(BinaryReader reader, List<string> nameMap)
		{
			NameIndex = reader.ReadInt32();
			Name = nameMap[NameIndex];
			Index = reader.ReadInt32();
			SamplerIndex = reader.ReadInt32();
			MultiSampled = reader.ReadBoolean();
			Dim = reader.ReadByte();
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
            writer.Write(SamplerIndex);
            writer.Write(MultiSampled);
            writer.Write(Dim);
        }
    }
}
