using System.Text;

namespace RudeShaderMiddleman.Middleman
{
	internal partial class CompilerMiddleman
	{
		private void Preprocess()
		{
			Header header;
			int readBytes;
			int cnt;

			// First message (shader file contents)
			middlemanOutputLog.WriteLine("preprocess: Shader file contents");
			readBytes = ReadString(unityPipeStream, compilerPipeStream);

			// Second message (shader file directory)
			middlemanOutputLog.WriteLine("preprocess: Shader file directory");
			readBytes = ReadString(unityPipeStream, compilerPipeStream);

			// Third message (shader file name)
			middlemanOutputLog.WriteLine("preprocess: Shader file name");
			readBytes = ReadString(unityPipeStream, compilerPipeStream);

			header = ReadHeader(unityPipeStream, compilerPipeStream, false); // surface only
			header = ReadHeader(unityPipeStream, compilerPipeStream, false); // build platform
			header = ReadHeader(unityPipeStream, compilerPipeStream, false); // valid APIs

			// Fourth message (p keywords)
			middlemanOutputLog.WriteLine("preprocess: p keywords");
			header = ReadHeader(unityPipeStream, compilerPipeStream, false);
			cnt = header.first;

			for (int keywords = 0; keywords < cnt; keywords++)
			{
				readBytes = ReadString(unityPipeStream, compilerPipeStream);
			}

			// Fifth message (d keywords)
			middlemanOutputLog.WriteLine("preprocess: d keywords");
			header = ReadHeader(unityPipeStream, compilerPipeStream, false);
			cnt = header.first;

			for (int keywords = 0; keywords < cnt; keywords++)
			{
				readBytes = ReadString(unityPipeStream, compilerPipeStream);
			}

			// Feedback
			while (true)
			{
				readBytes = ReadString(compilerPipeStream, unityPipeStream);
				string line = Encoding.UTF8.GetString(buff, 0, readBytes);
				middlemanOutputLog.WriteLine(line);

				if (line.StartsWith("shader:"))
					break;

				if (line.StartsWith("keywordsEnd:"))
				{
					readBytes = ReadString(compilerPipeStream, unityPipeStream, true);
					readBytes = ReadString(compilerPipeStream, unityPipeStream, true);
					readBytes = ReadString(compilerPipeStream, unityPipeStream, true);
					ReadHeader(compilerPipeStream, unityPipeStream, true);

					middlemanOutputLog.WriteLine("preprocess: Other keywords");
					header = ReadHeader(compilerPipeStream, unityPipeStream, false);
					cnt = header.first;

					for (int i = 0; i < cnt; i++)
					{
						readBytes = ReadString(compilerPipeStream, unityPipeStream);
						middlemanOutputLog.WriteLine(Encoding.UTF8.GetString(buff, 0, readBytes));
						ReadHeader(compilerPipeStream, unityPipeStream, true);
					}

					continue;
				}

			}
			
			// processed shader code
			middlemanOutputLog.WriteLine("preprocess: Processed shader code");
			readBytes = ReadString(compilerPipeStream, unityPipeStream);
		}
	}
}
