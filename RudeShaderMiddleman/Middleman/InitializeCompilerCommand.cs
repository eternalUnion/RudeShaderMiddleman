using System.Text;

namespace RudeShaderMiddleman.Middleman
{
	internal partial class CompilerMiddleman
	{
		private void InitializeCompiler()
		{
			Header header;
			int readBytes;
			int cnt;

			// First message
			header = ReadHeader(unityPipeStream, compilerPipeStream, false);
			cnt = header.first;
			for (int i = 0; i < cnt; i++)
			{
				readBytes = ReadString(unityPipeStream, compilerPipeStream);
			}

			// Second message
			header = ReadHeader(unityPipeStream, compilerPipeStream, false);
			cnt = header.first;
			for (int i = 0; i < cnt * 2; i++)
			{
				readBytes = ReadString(unityPipeStream, compilerPipeStream);
			}

			// Terminate
			header = ReadHeader(unityPipeStream, compilerPipeStream, true);

			// Feedback
			header = ReadHeader(compilerPipeStream, unityPipeStream, false);

			for (int messages = 0; messages < 26; messages++)
			{
				header = ReadHeader(compilerPipeStream, unityPipeStream, true);
				header = ReadHeader(compilerPipeStream, unityPipeStream, false);
			}
		}
	}
}
