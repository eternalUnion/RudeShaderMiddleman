using RudeShaderMiddleman.Metadata;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RudeShaderMiddleman.Middleman
{
	internal partial class CompilerMiddleman
	{
		private static Regex passNameRegex = new Regex(@"^<Unnamed Pass (\d+)>$");

		private void WriteBindings(ShaderPass passEntry, VariantEntry variantEntry, bool isVertexShader)
		{
			foreach (var binding in variantEntry.inputBindings.OrderBy(b => b.Source))
			{
				WriteString(unityPipeStream, $"input: {binding.Source} {(int)binding.Target}");
				middlemanOutputLog.WriteLine($"Binding = input: {binding.Source} {(int)binding.Target}");
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
				middlemanOutputLog.WriteLine($"Binding = cb: {cb.Name} {cb.UsedSize} {parameters.Count}");

				foreach (var param in parameters.OrderBy(p => p.Index))
				{
					WriteString(unityPipeStream, $"const: {param.ParamName} {param.Index} 0 {(param.IsMatrix ? 1 : 0)} {param.Rows} {param.Columns} {param.ArraySize}");
					middlemanOutputLog.WriteLine($"Binding = const: {param.ParamName} {param.Index} 0 {(param.IsMatrix ? 1 : 0)} {param.Rows} {param.Columns} {param.ArraySize}");
				}

				middlemanOutputLog.WriteLine($"Struct param count: {cb.StructParams.Count} : {(partialCb == null ? -1 : partialCb.StructParams.Count)}");
			}

			List<ConstantBufferBinding> cbBindings = new List<ConstantBufferBinding>(variantEntry.parameters.ConstBindings);
			cbBindings.AddRange(commonParams.ConstBindings.Where(binding => !cbBindings.Any(b => b.Name == binding.Name)).ToArray());

			foreach (var cbBinding in cbBindings.OrderBy(b => b.Index))
			{
				WriteString(unityPipeStream, $"cbbind: {cbBinding.Name} {cbBinding.Index}");
				middlemanOutputLog.WriteLine($"Binding = cbbind: {cbBinding.Name} {cbBinding.Index}");
			}

			List<TextureParameter> textures = new List<TextureParameter>(variantEntry.parameters.TextureParameters);
			textures.AddRange(commonParams.TextureParameters.Where(texture => !textures.Any(t => t.Name == texture.Name)));

			foreach (var tex in textures.OrderBy(t => t.Index))
			{
				WriteString(unityPipeStream, $"texbind: {tex.Name} {tex.Index} {tex.SamplerIndex} {(tex.MultiSampled ? 1 : 0)} {tex.Dim}");
				middlemanOutputLog.WriteLine($"Binding = texbind: {tex.Name} {tex.Index} {tex.SamplerIndex} {(tex.MultiSampled ? 1 : 0)} {tex.Dim}");
			}

			List<ConstantBufferBinding> buffers = new List<ConstantBufferBinding>(variantEntry.parameters.Buffers);
			buffers.AddRange(commonParams.Buffers.Where(buff => !buffers.Any(b => b.Name == buff.Name)));

			foreach (var buff in buffers.OrderBy(buff => buff.Index))
			{
				WriteString(unityPipeStream, $"bufferbind: {buff.Name} {buff.Index} {buff.ArraySize}");
				middlemanOutputLog.WriteLine($"Binding = bufferbind: {buff.Name} {buff.Index} {buff.ArraySize}");
			}

			List<SamplerParameter> samplers = new List<SamplerParameter>(variantEntry.parameters.Samplers);
			samplers.AddRange(commonParams.Samplers.Where(samp => !samplers.Any(s => s.Sampler == samp.Sampler)));

			foreach (var sampler in samplers.OrderBy(s => s.BindPoint))
			{
				WriteString(unityPipeStream, $"sampler: {sampler.Sampler} {sampler.BindPoint}");
				middlemanOutputLog.WriteLine($"Binding = sampler: {sampler.Sampler} {sampler.BindPoint}");
			}

			WriteString(unityPipeStream, $"stats: {variantEntry.statsAlu} {variantEntry.statsTex} {variantEntry.statsFlow} {variantEntry.statsTempRegister}");
			middlemanOutputLog.WriteLine($"Binding = stats: {variantEntry.statsAlu} {variantEntry.statsTex} {variantEntry.statsFlow} {variantEntry.statsTempRegister}");
		}

		private void CompileSnippet()
		{
			Header header;
			int readBytes;
			int cnt;

			// Processed shader code
			middlemanOutputLog.WriteLine("compileSnippet: Shader code");
			readBytes = ReadString(unityPipeStream, compilerPipeStream);

			// Shader file directory
			middlemanOutputLog.WriteLine("compileSnippet: Shader directory");
			readBytes = ReadString(unityPipeStream, compilerPipeStream);
			string shaderDir = Encoding.UTF8.GetString(buff, 0, readBytes);

			// Shader file name
			middlemanOutputLog.WriteLine("compileSnippet: Shader file");
			readBytes = ReadString(unityPipeStream, compilerPipeStream);
			string shaderFileName = Encoding.UTF8.GetString(buff, 0, readBytes);

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
					middlemanOutputLog.WriteLine($"compileSnippet: Derived shader guid = '{guid}'");
				}
			}
			else
			{
				middlemanOutputLog.WriteLine($"compileSnippet: Could not locate meta file at '{metaPath}'");
			}

			// Pass name
			middlemanOutputLog.WriteLine("compileSnippet: Shader pass");
			readBytes = ReadString(unityPipeStream, compilerPipeStream);
			string passName = Encoding.UTF8.GetString(buff, 0, readBytes);

			middlemanOutputLog.WriteLine("compileSnippet: 4 unknown");
			ReadHeader(unityPipeStream, compilerPipeStream, false);
			ReadHeader(unityPipeStream, compilerPipeStream, false);
			ReadHeader(unityPipeStream, compilerPipeStream, false);
			ReadHeader(unityPipeStream, compilerPipeStream, false);

			// ??? Keywords
			middlemanOutputLog.WriteLine("compileSnippet: Unknown keyword array");
			header = ReadHeader(unityPipeStream, compilerPipeStream, false);
			cnt = header.first;

			for (int i = 0; i < cnt; i++)
			{
				readBytes = ReadString(unityPipeStream, compilerPipeStream);
			}

			// Enabled shader keywords
			middlemanOutputLog.WriteLine("compileSnippet: Enabled keywords");
			header = ReadHeader(unityPipeStream, compilerPipeStream, false);
			cnt = header.first;

			List<string> enabledKeywords = new List<string>();
			for (int i = 0; i < cnt; i++)
			{
				readBytes = ReadString(unityPipeStream, compilerPipeStream);
				enabledKeywords.Add(Encoding.UTF8.GetString(buff, 0, readBytes));
			}

			middlemanOutputLog.WriteLine($"compileSnippet: Enabled keywords = {string.Join(" ", enabledKeywords)}");

			// Disabled shader keywords
			middlemanOutputLog.WriteLine("compileSnippet: Disabled keywords");
			header = ReadHeader(unityPipeStream, compilerPipeStream, false);
			cnt = header.first;

			List<string> disabledKeywords = new List<string>();
			for (int i = 0; i < cnt; i++)
			{
				readBytes = ReadString(unityPipeStream, compilerPipeStream);
				disabledKeywords.Add(Encoding.UTF8.GetString(buff, 0, readBytes));
			}

			middlemanOutputLog.WriteLine($"compileSnippet: Disabled keywords = {string.Join(" ", disabledKeywords)}");

			middlemanOutputLog.WriteLine("compileSnippet: reading compiler output");
			ReadHeader(unityPipeStream, compilerPipeStream, false);

			// flags=0
			ReadHeader(unityPipeStream, compilerPipeStream, true);
			// lang=0
			ReadHeader(unityPipeStream, compilerPipeStream, false);
			// type=Vertex|Fragment
			header = ReadHeader(unityPipeStream, compilerPipeStream, false);
			bool vertexShader = header.first == 0;
			middlemanOutputLog.WriteLine($"Shader type: {(vertexShader ? "Vertex" : "Fragment")}");
			// platform=BuildStandaloneWin64Player (19)
			ReadHeader(unityPipeStream, compilerPipeStream, false);
			// reqs=4075 (?)
			ReadHeader(unityPipeStream, compilerPipeStream, true);
			// mask=6
			ReadHeader(unityPipeStream, compilerPipeStream, false);
			// start=194
			ReadHeader(unityPipeStream, compilerPipeStream, false);

			bool shaderReplaced = false;

			// Injection
			Match passNameMatch = passNameRegex.Match(passName);
			if (!passNameMatch.Success)
				middlemanOutputLog.WriteLine($"Invalid pass name: {passName}");

			if (passNameMatch.Success && guid != null && shaders.TryGetValue(guid, out ShaderEntry entry))
			{
				int passNum = int.Parse(passNameMatch.Groups[1].Value);
				ShaderPass passEntry = entry.shaderPasses.Where(p => p.passNum == passNum).FirstOrDefault();

				middlemanOutputLog.WriteLine($"Shader found in the table. Attempting to find suitable variant");

				if (passEntry == null)
				{
					middlemanOutputLog.WriteLine($"Shader pass not found");
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
							middlemanOutputLog.WriteLine($"Fallback variant");
						}
					}
					else
					{
						enabledBitmask = enabledBitmask & passEntry.fragmentKeywordMask;
						matchedVariant = passEntry.fragmentVariants.Where(v => (v.keywords & enabledBitmask) == enabledBitmask).FirstOrDefault();
						if (matchedVariant == null)
						{
							matchedVariant = passEntry.fragmentVariants.Last();
							middlemanOutputLog.WriteLine($"Fallback variant");
						}
					}

					ZipArchiveEntry blob = blobs.GetEntry($"{guid}/{matchedVariant.offset}");
					if (blob == null)
					{
						middlemanOutputLog.WriteLine("Could not locate blob entry");
					}
					else
					{
						WriteBindings(passEntry, matchedVariant, vertexShader);

						// Ignore compiler output
						while (true)
						{
							readBytes = ReadString(compilerPipeStream, unityPipeStream, false);
							string binding = Encoding.UTF8.GetString(buff, 0, readBytes);
							middlemanOutputLog.WriteLine($"(SKIP) compileSnippet: Binding = '{binding}'");

							if (Encoding.UTF8.GetString(buff, 0, readBytes).StartsWith("shader: "))
							{
								break;
							}
						}

						header = ReadHeader(compilerPipeStream, unityPipeStream, false, false);

						middlemanOutputLog.WriteLine("(SKIP) compileSnippet: Shader bytecode");
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
					middlemanOutputLog.WriteLine($"compileSnippet:  Binding = '{binding}'");

					if (Encoding.UTF8.GetString(buff, 0, readBytes).StartsWith("shader: "))
					{
						break;
					}
				}

				header = ReadHeader(compilerPipeStream, unityPipeStream, false);

				middlemanOutputLog.WriteLine("compileSnippet: Shader bytecode");
				readBytes = ReadString(compilerPipeStream, unityPipeStream);
			}
		}
	}
}
