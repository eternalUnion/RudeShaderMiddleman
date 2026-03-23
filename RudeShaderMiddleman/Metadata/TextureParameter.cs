using System.IO;

namespace RudeShaderMiddleman.Metadata
{
    public class TextureParameter
    {
        public string Name;
        public int Index;
        public int SamplerIndex;
        public bool MultiSampled;
        public byte Dim;

		public TextureParameter(BinaryReader reader)
		{
			Name = reader.ReadString();
			Index = reader.ReadInt32();
			SamplerIndex = reader.ReadInt32();
			MultiSampled = reader.ReadBoolean();
			Dim = reader.ReadByte();
		}

		public void Serialize(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(Index);
            writer.Write(SamplerIndex);
            writer.Write(MultiSampled);
            writer.Write(Dim);
        }
    }
}
