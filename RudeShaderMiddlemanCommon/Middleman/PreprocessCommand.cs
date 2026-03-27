using System.Text;

namespace RudeShadermiddlemanCommon.Middleman
{
	public partial class CompilerMiddleman
	{
		private void Preprocess()
		{
			Header header;
			int readBytes;
			int cnt;

			// First message (shader file contents)
			Log("Shader file contents", LogLevel.DEBUG);
			readBytes = ReadString(unityPipeStream, compilerPipeStream);

			// Second message (shader file directory)
			Log("Shader file directory", LogLevel.DEBUG);
			readBytes = ReadString(unityPipeStream, compilerPipeStream);
			string shaderDir = Encoding.UTF8.GetString(buff, 0, readBytes);

			// Third message (shader file name)
			Log("Shader file name", LogLevel.DEBUG);
			readBytes = ReadString(unityPipeStream, compilerPipeStream);
			string shaderFileName = Encoding.UTF8.GetString(buff, 0, readBytes);

			Log($"Preprocessing {shaderDir}/{shaderFileName}");

			header = ReadHeader(unityPipeStream, compilerPipeStream, false); // surface only
			header = ReadHeader(unityPipeStream, compilerPipeStream, false); // build platform
			header = ReadHeader(unityPipeStream, compilerPipeStream, false); // valid APIs

			// Fourth message (p keywords)
			Log("p keywords", LogLevel.DEBUG);
			header = ReadHeader(unityPipeStream, compilerPipeStream, false);
			cnt = header.first;

			for (int keywords = 0; keywords < cnt; keywords++)
			{
				readBytes = ReadString(unityPipeStream, compilerPipeStream);
			}

			// Fifth message (d keywords)
			Log("d keywords", LogLevel.DEBUG);
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
				Log(line, LogLevel.DEBUG);

				if (line.StartsWith("shader:"))
					break;

				if (line.StartsWith("keywordsEnd:"))
				{
					readBytes = ReadString(compilerPipeStream, unityPipeStream, true);
					readBytes = ReadString(compilerPipeStream, unityPipeStream, true);
					readBytes = ReadString(compilerPipeStream, unityPipeStream, true);
					ReadHeader(compilerPipeStream, unityPipeStream, true);

					Log("Other keywords", LogLevel.DEBUG);
					header = ReadHeader(compilerPipeStream, unityPipeStream, false);
					cnt = header.first;

					for (int i = 0; i < cnt; i++)
					{
						readBytes = ReadString(compilerPipeStream, unityPipeStream);
						Log(Encoding.UTF8.GetString(buff, 0, readBytes), LogLevel.DEBUG);
						ReadHeader(compilerPipeStream, unityPipeStream, true);
					}

					continue;
				}

			}
			
			// processed shader code
			Log("Processed shader code", LogLevel.DEBUG);
			readBytes = ReadString(compilerPipeStream, unityPipeStream);
		}
	}
}
