using RudeShadermiddleman.Common.Metadata;
using System.Collections.Generic;
using System.IO;

namespace RudeShadermiddleman.Common.ShaderTable
{
	public class VariantEntry
	{
		// Info
		public int type;
		public long keywords;
		public int sourceMap;

		// Blob location
		public int offset;
		public int length;

		// Status
		public int statsAlu;
		public int statsTex;
		public int statsFlow;
		public int statsTempRegister;

		// Bindings
		public List<BindChannel> inputBindings;
		public ShaderParameters parameters;

		public VariantEntry() { }

		public VariantEntry(BinaryReader reader)
		{
			type = reader.ReadInt32();
			keywords = reader.ReadInt64();
			sourceMap = reader.ReadInt32();
			offset = reader.ReadInt32();
			length = reader.ReadInt32();
			statsAlu = reader.ReadInt32();
			statsTex = reader.ReadInt32();
			statsFlow = reader.ReadInt32();
			statsTempRegister = reader.ReadInt32();

			inputBindings = new List<BindChannel>();
			int inputCount = reader.ReadInt32();
			for (int i = 0; i < inputCount; i++)
			{
				inputBindings.Add(new BindChannel(reader));
			}

			parameters = new ShaderParameters(reader);
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(type);
			writer.Write(keywords);
			writer.Write(sourceMap);
			writer.Write(offset);
			writer.Write(length);
			writer.Write(statsAlu);
			writer.Write(statsTex);
			writer.Write(statsFlow);
			writer.Write(statsTempRegister);

			writer.Write((int)inputBindings.Count);
			foreach (BindChannel input in inputBindings)
			{
				input.Serialize(writer);
			}

			parameters.Serialize(writer);
		}
	}
}
