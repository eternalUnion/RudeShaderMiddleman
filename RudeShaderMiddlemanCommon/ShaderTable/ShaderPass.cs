using RudeShadermiddlemanCommon.Metadata;
using System.Collections.Generic;
using System.IO;

namespace RudeShadermiddlemanCommon.ShaderTable
{
	public class ShaderPass
	{
		public int passNum;
		public long vertexKeywordMask;
		public long fragmentKeywordMask;

		public ShaderParameters vertexCommonParameters;
		public ShaderParameters fragmentCommonParameters;

		public List<VariantEntry> vertexVariants = new List<VariantEntry>();

		public List<VariantEntry> fragmentVariants = new List<VariantEntry>();

		public ShaderPass() { }

		public ShaderPass(BinaryReader reader)
		{
			passNum = reader.ReadInt32();
			vertexKeywordMask = reader.ReadInt64();
			fragmentKeywordMask = reader.ReadInt64();

			vertexCommonParameters = new ShaderParameters(reader);
			fragmentCommonParameters = new ShaderParameters(reader);

			vertexVariants = new List<VariantEntry>();
			int vertexVarCount = reader.ReadInt32();
			for (int i = 0; i < vertexVarCount; i++)
			{
				vertexVariants.Add(new VariantEntry(reader));
			}

			fragmentVariants = new List<VariantEntry>();
			int fragmentVarCount = reader.ReadInt32();
			for (int i = 0; i < fragmentVarCount; i++)
			{
				fragmentVariants.Add(new VariantEntry(reader));
			}
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(passNum);
			writer.Write(vertexKeywordMask);
			writer.Write(fragmentKeywordMask);

			vertexCommonParameters.Serialize(writer);
			fragmentCommonParameters.Serialize(writer);

			writer.Write((int)vertexVariants.Count);
			foreach (var vertexVar in vertexVariants)
			{
				vertexVar.Serialize(writer);
			}

			writer.Write((int)fragmentVariants.Count);
			foreach (var fragmentVar in fragmentVariants)
			{
				fragmentVar.Serialize(writer);
			}
		}
	}
}
