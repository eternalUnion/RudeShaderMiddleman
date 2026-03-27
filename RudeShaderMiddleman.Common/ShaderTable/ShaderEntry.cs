using System.Collections.Generic;
using System.IO;

namespace RudeShaderMiddleman.Common.ShaderTable
{
	public class ShaderEntry
	{
		public string guid = "";
		public List<string> shaderKeywords = new List<string>();
		public List<ShaderPass> shaderPasses = new List<ShaderPass>();

		public ShaderEntry() { }

		public ShaderEntry(BinaryReader reader)
		{
			guid = reader.ReadString();

			shaderKeywords = new List<string>();
			int keywordCount = reader.ReadInt32();
			for (int i = 0; i < keywordCount; i++)
			{
				shaderKeywords.Add(reader.ReadString());
			}

			shaderPasses = new List<ShaderPass>();
			int passCount = reader.ReadInt32();
			for (int i = 0; i < passCount; i++)
			{
				shaderPasses.Add(new ShaderPass(reader));
			}
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(guid);

			writer.Write((int)shaderKeywords.Count);
			foreach (string keyword in shaderKeywords)
			{
				writer.Write(keyword);
			}

			writer.Write((int)shaderPasses.Count);
			foreach (ShaderPass pass in shaderPasses)
			{
				pass.Serialize(writer);
			}
		}
	}
}
