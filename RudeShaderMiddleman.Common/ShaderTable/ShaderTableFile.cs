using RudeShaderMiddleman.Common.ShaderTable.Enums;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace RudeShaderMiddleman.Common.ShaderTable
{
	public class ShaderTableFile : IDisposable
	{
		private ZipArchive table;

		public ShaderTable shaderTable;

		public ShaderTableFile(ZipArchive archive)
		{
			table = archive;

			var tableEntry = table.GetEntry("table.bin");
			if (tableEntry == null)
			{
				shaderTable = new ShaderTable();
			}
			else
			{
				using (BinaryReader tableReader = new BinaryReader(tableEntry.Open()))
				{
					shaderTable = new ShaderTable(tableReader);
				}
			}
		}

		public void AddShaderEntry(string guid, ShaderEntry shaderEntry)
		{
			if (shaderTable == null)
				shaderTable = new ShaderTable();

			shaderTable.shaders[guid] = shaderEntry;
		}

		public ShaderEntry GetShaderEntry(string guid)
		{
			if (shaderTable.shaders.TryGetValue(guid, out var shaderEntry))
				return shaderEntry;
			return null;
		}

		public ShaderEntry this[string guid]
		{
			get => GetShaderEntry(guid);
		}

		public VariantEntryEnumerator GetVariantEnumerator(string guid, int pass, ShaderGpuProgramType programType)
		{
			if (guid == null)
				return null;

			ShaderEntry shaderEntry = GetShaderEntry(guid);
			if (shaderEntry == null)
				return null;

			if (!shaderEntry.shaderPasses.Any(p => p.passNum == pass))
				return null;

			ZipArchiveEntry variantEntry = null;

			switch (programType)
			{
				case ShaderGpuProgramType.DX11VertexSM40:
				case ShaderGpuProgramType.DX11VertexSM50:
					variantEntry = table.GetEntry($"{guid}/pass_{pass}/dx11_vert.bin");
					break;

				case ShaderGpuProgramType.DX11PixelSM40:
				case ShaderGpuProgramType.DX11PixelSM50:
					variantEntry = table.GetEntry($"{guid}/pass_{pass}/dx11_frag.bin");
					break;

				case ShaderGpuProgramType.GLCore32:
				case ShaderGpuProgramType.GLCore41:
				case ShaderGpuProgramType.GLCore43:
					variantEntry = table.GetEntry($"{guid}/pass_{pass}/glcore.bin");
					break;
			}

			if (variantEntry == null)
				return null;

			return new VariantEntryEnumerator(new BinaryReader(variantEntry.Open()), shaderTable.nameMap);
		}

		public void RewriteShaderTable()
		{
			var tableEntry = table.GetEntry("table.bin");
			if (tableEntry != null)
				tableEntry.Delete();
			
			tableEntry = table.CreateEntry("table.bin");
			
			using (BinaryWriter writer = new BinaryWriter(tableEntry.Open()))
			{
				shaderTable.Serialize(writer);
			}
		}

		public void Dispose()
		{
			table.Dispose();
		}
	}
}
