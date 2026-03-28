using RudeShaderMiddleman.Common.Metadata;
using System.Collections.Generic;
using System.IO;

namespace RudeShaderMiddleman.Common.ShaderTable
{
	public class ShaderPass
	{
		public int passNum;
		public long vertexKeywordMask;
		public long fragmentKeywordMask;

		public ShaderParameters vertexCommonParameters;
		public ShaderParameters fragmentCommonParameters;

		public ShaderPass() { }

		public ShaderPass(BinaryReader reader, List<string> nameMap)
		{
			passNum = reader.ReadInt32();
			vertexKeywordMask = reader.ReadInt64();
			fragmentKeywordMask = reader.ReadInt64();

			vertexCommonParameters = new ShaderParameters(reader, nameMap);
			fragmentCommonParameters = new ShaderParameters(reader, nameMap);
		}

		public void Serialize(BinaryWriter writer, List<string> nameMap)
		{
			writer.Write(passNum);
			writer.Write(vertexKeywordMask);
			writer.Write(fragmentKeywordMask);

			vertexCommonParameters.Serialize(writer, nameMap);
			fragmentCommonParameters.Serialize(writer, nameMap);
		}
	}
}
