using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RudeShaderMiddleman.Common.ShaderTable
{
	public class ShaderTable
	{
		public List<string> nameMap = new List<string>();
		public Dictionary<string, ShaderEntry> shaders = new Dictionary<string, ShaderEntry>();

		public ShaderTable() { }

		public ShaderTable(BinaryReader reader)
		{
			int nameCnt = reader.ReadInt32();
			nameMap.Clear();
			for (int i = 0; i < nameCnt; i++)
			{
				nameMap.Add(reader.ReadString());
			}

			shaders = new Dictionary<string, ShaderEntry>();
			int cnt = reader.ReadInt32();
			for (int i = 0; i < cnt; i++)
			{
				string guid = reader.ReadString();
				ShaderEntry entry = new ShaderEntry(reader, nameMap);
				shaders[guid] = entry;
			}
		}

		public void Serialize(BinaryWriter writer)
		{
			// Need to get all names first
			using (MemoryStream temp = new MemoryStream())
			{
				BinaryWriter tempWriter = new BinaryWriter(temp, Encoding.UTF8, true);

				tempWriter.Write(shaders.Count);
				foreach (var pair in shaders)
				{
					tempWriter.Write(pair.Key);
					pair.Value.Serialize(tempWriter, nameMap);
				}
				tempWriter.Close();

				writer.Write(nameMap.Count);
				foreach (string name in nameMap)
				{
					writer.Write(name);
				}
				writer.Flush();

				temp.Position = 0;
				temp.CopyTo(writer.BaseStream);
			}
		}
	}
}
