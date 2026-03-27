using RudeShadermiddleman.Common.Metadata;
using RudeShadermiddleman.Common.ShaderTable;
using RudeShadermiddleman.Common.ShaderTable.Enums;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RudeShadermiddleman.Common.Middleman
{
	public partial class CompilerMiddleman
	{
		private static Regex passNameRegex = new Regex(@"^<Unnamed Pass (\d+)>$");

		private void WriteBindings(ShaderPass passEntry, VariantEntry variantEntry, bool isVertexShader)
		{
			foreach (var binding in variantEntry.inputBindings.OrderBy(b => b.Source))
			{
				WriteString(unityPipeStream, $"input: {binding.Source} {(int)binding.Target}");
				Log($"Binding = input: {binding.Source} {(int)binding.Target}", LogLevel.DEBUG);
			}

			ShaderParameters commonParams = isVertexShader ? passEntry.vertexCommonParameters : passEntry.fragmentCommonParameters;

			// Variant parameters

			IEnumerable<ConstantBuffer> buffs = (variantEntry.parameters.BaseConstantBuffer == null || string.IsNullOrEmpty(variantEntry.parameters.BaseConstantBuffer.Name))
				? variantEntry.parameters.ConstantBuffers
				: new ConstantBuffer[] { variantEntry.parameters.BaseConstantBuffer }.Concat(variantEntry.parameters.ConstantBuffers);

			foreach (var cb in buffs)
			{
				var partialCb = commonParams.ConstantBuffers.Where(c => c.Partial && c.Name == cb.Name).FirstOrDefault();

				List<ConstantBufferParameter> parameters = new List<ConstantBufferParameter>(cb.CBParams);
				if (partialCb != null)
					parameters.AddRange(partialCb.CBParams);

				WriteString(unityPipeStream, $"cb: {cb.Name} {cb.UsedSize} {parameters.Count}");
				Log($"Binding = cb: {cb.Name} {cb.UsedSize} {parameters.Count}", LogLevel.DEBUG);

				foreach (var param in parameters.OrderBy(p => p.Index))
				{
					WriteString(unityPipeStream, $"const: {param.ParamName} {param.Index} 0 {(param.IsMatrix ? 1 : 0)} {param.Rows} {param.Columns} {param.ArraySize}");
					Log($"Binding = const: {param.ParamName} {param.Index} 0 {(param.IsMatrix ? 1 : 0)} {param.Rows} {param.Columns} {param.ArraySize}", LogLevel.DEBUG);
				}

				Log($"Struct param count: {cb.StructParams.Count} : {(partialCb == null ? -1 : partialCb.StructParams.Count)}", LogLevel.DEBUG);
			}

			List<ConstantBufferBinding> cbBindings = new List<ConstantBufferBinding>(variantEntry.parameters.ConstBindings);
			cbBindings.AddRange(commonParams.ConstBindings.Where(binding => !cbBindings.Any(b => b.Name == binding.Name)).ToArray());

			foreach (var cbBinding in cbBindings.OrderBy(b => b.Index))
			{
				WriteString(unityPipeStream, $"cbbind: {cbBinding.Name} {cbBinding.Index}");
				Log($"Binding = cbbind: {cbBinding.Name} {cbBinding.Index}", LogLevel.DEBUG);
			}

			List<TextureParameter> textures = new List<TextureParameter>(variantEntry.parameters.TextureParameters);
			textures.AddRange(commonParams.TextureParameters.Where(texture => !textures.Any(t => t.Name == texture.Name)));

			foreach (var tex in textures.OrderBy(t => t.Index))
			{
				WriteString(unityPipeStream, $"texbind: {tex.Name} {tex.Index} {tex.SamplerIndex} {(tex.MultiSampled ? 1 : 0)} {tex.Dim}");
				Log($"Binding = texbind: {tex.Name} {tex.Index} {tex.SamplerIndex} {(tex.MultiSampled ? 1 : 0)} {tex.Dim}", LogLevel.DEBUG);
			}

			List<ConstantBufferBinding> buffers = new List<ConstantBufferBinding>(variantEntry.parameters.Buffers);
			buffers.AddRange(commonParams.Buffers.Where(buff => !buffers.Any(b => b.Name == buff.Name)));

			foreach (var buff in buffers.OrderBy(buff => buff.Index))
			{
				WriteString(unityPipeStream, $"bufferbind: {buff.Name} {buff.Index} {buff.ArraySize}");
				Log($"Binding = bufferbind: {buff.Name} {buff.Index} {buff.ArraySize}", LogLevel.DEBUG);
			}

			List<SamplerParameter> samplers = new List<SamplerParameter>(variantEntry.parameters.Samplers);
			samplers.AddRange(commonParams.Samplers.Where(samp => !samplers.Any(s => s.Sampler == samp.Sampler)));

			foreach (var sampler in samplers.OrderBy(s => s.BindPoint))
			{
				WriteString(unityPipeStream, $"sampler: {sampler.Sampler} {sampler.BindPoint}");
				Log($"Binding = sampler: {sampler.Sampler} {sampler.BindPoint}", LogLevel.DEBUG);
			}

			WriteString(unityPipeStream, $"stats: {variantEntry.statsAlu} {variantEntry.statsTex} {variantEntry.statsFlow} {variantEntry.statsTempRegister}");
			Log($"Binding = stats: {variantEntry.statsAlu} {variantEntry.statsTex} {variantEntry.statsFlow} {variantEntry.statsTempRegister}", LogLevel.DEBUG);
		}

		private void CompileSnippet()
		{
			Header header;
			int readBytes;
			int cnt;

			// Processed shader code
			Log("Shader code", LogLevel.DEBUG);
			readBytes = ReadString(unityPipeStream, compilerPipeStream);

			// Shader file directory
			Log("Shader directory", LogLevel.DEBUG);
			readBytes = ReadString(unityPipeStream, compilerPipeStream);
			string shaderDir = Encoding.UTF8.GetString(buff, 0, readBytes);

			// Shader file name
			Log("Shader file", LogLevel.DEBUG);
			readBytes = ReadString(unityPipeStream, compilerPipeStream);
			string shaderFileName = Encoding.UTF8.GetString(buff, 0, readBytes);
			Log($"Compiling {shaderDir}/{shaderFileName}");

			// Get guid from the file
			string metaPath = Path.Combine(shaderDir, shaderFileName + ".meta");
			string guid = null;

			if (File.Exists(metaPath))
			{
				string metaContens;
				using (StreamReader reader = new StreamReader(File.Open(metaPath, FileMode.Open, FileAccess.Read, FileShare.Read)))
					metaContens = reader.ReadToEnd();

				int guidIndex = metaContens.IndexOf("guid: ");
				if (guidIndex != -1)
				{
					guid = metaContens.Substring(guidIndex + "guid: ".Length, 32);
					Log($"Derived shader guid = '{guid}'", LogLevel.DEBUG);
				}
			}
			else
			{
				Log($"Could not locate meta file at '{metaPath}'");
			}

			// Pass name
			Log("Shader pass", LogLevel.DEBUG);
			readBytes = ReadString(unityPipeStream, compilerPipeStream);
			string passName = Encoding.UTF8.GetString(buff, 0, readBytes);
			Log($"Pass name: '{passName}'", LogLevel.DEBUG);

			Log("4 unknown", LogLevel.DEBUG);
			ReadHeader(unityPipeStream, compilerPipeStream, false);
			ReadHeader(unityPipeStream, compilerPipeStream, false);
			ReadHeader(unityPipeStream, compilerPipeStream, false);
			ReadHeader(unityPipeStream, compilerPipeStream, false);

			// ??? Keywords
			Log("Unknown keyword array", LogLevel.DEBUG);
			header = ReadHeader(unityPipeStream, compilerPipeStream, false);
			cnt = header.first;

			for (int i = 0; i < cnt; i++)
			{
				readBytes = ReadString(unityPipeStream, compilerPipeStream);
			}

			// Enabled shader keywords
			Log("Enabled keywords", LogLevel.DEBUG);
			header = ReadHeader(unityPipeStream, compilerPipeStream, false);
			cnt = header.first;

			List<string> enabledKeywords = new List<string>();
			for (int i = 0; i < cnt; i++)
			{
				readBytes = ReadString(unityPipeStream, compilerPipeStream);
				enabledKeywords.Add(Encoding.UTF8.GetString(buff, 0, readBytes));
			}

			Log($"Enabled keywords = {string.Join(" ", enabledKeywords)}", LogLevel.DEBUG);

			// Disabled shader keywords
			Log("Disabled keywords", LogLevel.DEBUG);
			header = ReadHeader(unityPipeStream, compilerPipeStream, false);
			cnt = header.first;

			List<string> disabledKeywords = new List<string>();
			for (int i = 0; i < cnt; i++)
			{
				readBytes = ReadString(unityPipeStream, compilerPipeStream);
				disabledKeywords.Add(Encoding.UTF8.GetString(buff, 0, readBytes));
			}

			Log($"Disabled keywords = {string.Join(" ", disabledKeywords)}", LogLevel.DEBUG);

			Log("reading compiler output", LogLevel.DEBUG);
			ReadHeader(unityPipeStream, compilerPipeStream, false);

			// flags=0
			ReadHeader(unityPipeStream, compilerPipeStream, true);
			// lang=3
			ReadHeader(unityPipeStream, compilerPipeStream, false);
			// type=Vertex|Fragment
			header = ReadHeader(unityPipeStream, compilerPipeStream, false);
			bool vertexShader = header.first == 0;
			Log($"Shader type: {(vertexShader ? "Vertex" : "Fragment")}");
			// platform= d3d11 | glcore | vulkan
			header = ReadHeader(unityPipeStream, compilerPipeStream, false);
			GPUPlatform platform = (GPUPlatform)header.first;
			Log($"GPU platform: {platform}");
			// reqs=4075 (?)
			ReadHeader(unityPipeStream, compilerPipeStream, true);
			// mask=6
			ReadHeader(unityPipeStream, compilerPipeStream, false);
			// start=194
			ReadHeader(unityPipeStream, compilerPipeStream, false);

			bool shaderReplaced = false;

			Match passNameMatch = passNameRegex.Match(passName);
			if (!passNameMatch.Success)
				Log($"Invalid pass name: {passName}");

			if (platform != GPUPlatform.d3d11)
				Log($"Warning: platform is not {GPUPlatform.d3d11}, will not inject ULTRAKILL shader");

			// Injection
			if (passNameMatch.Success && guid != null && shaders.TryGetValue(guid, out ShaderEntry entry) && platform == GPUPlatform.d3d11)
			{
				int passNum = int.Parse(passNameMatch.Groups[1].Value);
				ShaderPass passEntry = entry.shaderPasses.Where(p => p.passNum == passNum).FirstOrDefault();

				Log($"Shader found in the table. Attempting to find suitable variant...");

				if (passEntry == null)
				{
					Log($"Shader pass not found");
				}
				else
				{
					long enabledBitmask = 0;

					foreach (string enabledKeyword in enabledKeywords)
					{
						int keywordIdx = entry.shaderKeywords.IndexOf(enabledKeyword);
						if (keywordIdx == -1)
							continue;

						enabledBitmask |= (long)1 << keywordIdx;
					}

					VariantEntry matchedVariant = null;

					if (vertexShader)
					{
						enabledBitmask = enabledBitmask & passEntry.vertexKeywordMask;
						matchedVariant = passEntry.vertexVariants.Where(v => (v.keywords & enabledBitmask) == enabledBitmask).FirstOrDefault();
						if (matchedVariant == null)
						{
							matchedVariant = passEntry.vertexVariants.Last();
							Log($"Using fallback variant");
						}
					}
					else
					{
						enabledBitmask = enabledBitmask & passEntry.fragmentKeywordMask;
						matchedVariant = passEntry.fragmentVariants.Where(v => (v.keywords & enabledBitmask) == enabledBitmask).FirstOrDefault();
						if (matchedVariant == null)
						{
							matchedVariant = passEntry.fragmentVariants.Last();
							Log($"Using fallback variant");
						}
					}

					ZipArchiveEntry blob = blobs.GetEntry($"{guid}/{matchedVariant.offset}");
					if (blob == null)
					{
						Log("Could not locate blob entry");
					}
					else
					{
						WriteBindings(passEntry, matchedVariant, vertexShader);

						// Ignore compiler output
						while (true)
						{
							readBytes = ReadString(compilerPipeStream, unityPipeStream, false);
							string binding = Encoding.UTF8.GetString(buff, 0, readBytes);
							Log($"(SKIP) Binding = '{binding}'", LogLevel.DEBUG);

							if (Encoding.UTF8.GetString(buff, 0, readBytes).StartsWith("shader: "))
							{
								break;
							}
						}

						header = ReadHeader(compilerPipeStream, unityPipeStream, false, false);

						Log("(SKIP) Shader bytecode", LogLevel.DEBUG);
						readBytes = ReadString(compilerPipeStream, unityPipeStream, false);

						// Inject shader
						WriteString(unityPipeStream, "shader: 1");
						WriteHeader(unityPipeStream, matchedVariant.type);

						using (Stream blobStream = blob.Open())
						{
							Write(unityPipeStream, blobStream, matchedVariant.length);
						}

						shaderReplaced = true;
					}
				}
			}

			// Response in case no injection was done
			if (!shaderReplaced)
			{
				while (true)
				{
					readBytes = ReadString(compilerPipeStream, unityPipeStream);
					string binding = Encoding.UTF8.GetString(buff, 0, readBytes);
					Log($" Binding = '{binding}'", LogLevel.DEBUG);

					if (Encoding.UTF8.GetString(buff, 0, readBytes).StartsWith("shader: "))
					{
						break;
					}
				}

				header = ReadHeader(compilerPipeStream, unityPipeStream, false);

				Log("Shader bytecode", LogLevel.DEBUG);
				readBytes = ReadString(compilerPipeStream, unityPipeStream);
			}
		}
	}
}
