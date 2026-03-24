using System.Text;

namespace RudeShaderMiddleman.Middleman
{
	internal partial class CompilerMiddleman
	{
		private void PreprocessCompute()
		{
			Header header;
			int readBytes;
			int cnt;

			// Shader source code
			readBytes = ReadString(unityPipeStream, compilerPipeStream);

			// File name
			readBytes = ReadString(unityPipeStream, compilerPipeStream);

			ReadHeader(unityPipeStream, compilerPipeStream, false);
			ReadHeader(unityPipeStream, compilerPipeStream, false);

			header = ReadHeader(unityPipeStream, compilerPipeStream, false);
			cnt = header.first;
			for (int i = 0; i < cnt; i++)
			{
				ReadString(unityPipeStream, compilerPipeStream);
			}

			header = ReadHeader(unityPipeStream, compilerPipeStream, false);
			cnt = header.first;
			for (int i = 0; i < cnt; i++)
			{
				ReadString(unityPipeStream, compilerPipeStream);
			}

			while (true)
			{
				readBytes = ReadString(compilerPipeStream, unityPipeStream);
				string line = Encoding.UTF8.GetString(buff, 0, readBytes);
				middlemanOutputLog.WriteLine(line);

				if (line.StartsWith("endKernels:"))
				{
					// Read processed code
					ReadString(compilerPipeStream, unityPipeStream);

					ReadHeader(compilerPipeStream, unityPipeStream, false);
					ReadHeader(compilerPipeStream, unityPipeStream, false);
					ReadHeader(compilerPipeStream, unityPipeStream, false);

					break;
				}

				if (line.StartsWith("requirements:"))
				{
					ReadHeader(compilerPipeStream, unityPipeStream, true);

					header = ReadHeader(compilerPipeStream, unityPipeStream, false);
					cnt = header.first;
					for (int i = 0; i < cnt; i++)
					{
						ReadString(compilerPipeStream, unityPipeStream);
						ReadHeader(compilerPipeStream, unityPipeStream, true);
					}
				}
			}
		}
	}
}
