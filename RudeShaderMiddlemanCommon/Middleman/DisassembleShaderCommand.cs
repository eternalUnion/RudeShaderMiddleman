using System.Text;

namespace RudeShadermiddleman.Common.Middleman
{
	public partial class CompilerMiddleman
	{
		private void DisassembleShader()
		{
			int readBytes;

			// Shader name
			ReadString(unityPipeStream, compilerPipeStream);

			ReadHeader(unityPipeStream, compilerPipeStream, false);
			ReadHeader(unityPipeStream, compilerPipeStream, false);
			ReadHeader(unityPipeStream, compilerPipeStream, false);

			// Shader binary
			ReadString(unityPipeStream, compilerPipeStream);

			while (true)
			{
				readBytes = ReadString(compilerPipeStream, unityPipeStream);
				string line = Encoding.UTF8.GetString(buff, 0, readBytes);

				if (line.StartsWith("disasm:"))
					break;
			}

			ReadString(compilerPipeStream, unityPipeStream);
		}
	}
}
