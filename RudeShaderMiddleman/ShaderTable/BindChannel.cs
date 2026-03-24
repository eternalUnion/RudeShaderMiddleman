using System.IO;

namespace RudeShaderMiddleman
{
	struct BindChannel
	{
		public int Source;
		public VertexComponent Target;

		public BindChannel(int source, VertexComponent target)
		{
			Source = source;
			Target = target;
		}

		public BindChannel(int source, int target)
		{
			Source = source;
			Target = (VertexComponent)target;
		}

		public BindChannel(BinaryReader reader)
		{
			Source = reader.ReadInt32();
			Target = (VertexComponent)reader.ReadInt32();
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Source);
			writer.Write((int)Target);
		}
	}
}
