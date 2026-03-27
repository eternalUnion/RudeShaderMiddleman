using System.Text;

namespace RudeShadermiddlemanCommon.Middleman
{
	public partial class CompilerMiddleman
	{
		private void CompilerComputeKernelCommand()
		{
			Header header;
			int readBytes;
			int cnt;

			// Source code
			ReadString(unityPipeStream, compilerPipeStream);

			// File name
			ReadString(unityPipeStream, compilerPipeStream);

			// Main method name
			ReadString(unityPipeStream, compilerPipeStream);

			ReadHeader(unityPipeStream, compilerPipeStream, false);
			ReadHeader(unityPipeStream, compilerPipeStream, false);
			ReadHeader(unityPipeStream, compilerPipeStream, false);

			header = ReadHeader(unityPipeStream, compilerPipeStream, false);
			cnt = header.first;
			for (int i = 0; i < cnt; i++)
			{
				ReadString(unityPipeStream, compilerPipeStream);
				ReadString(unityPipeStream, compilerPipeStream);
			}

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

			ReadHeader(unityPipeStream, compilerPipeStream, false);
			ReadHeader(unityPipeStream, compilerPipeStream, true);
			ReadHeader(unityPipeStream, compilerPipeStream, true);
			ReadHeader(unityPipeStream, compilerPipeStream, false);
			ReadHeader(unityPipeStream, compilerPipeStream, false);

			while (true)
			{
				readBytes = ReadString(compilerPipeStream, unityPipeStream);
				string line = Encoding.UTF8.GetString(buff, 0, readBytes);
				middlemanOutputLog.WriteLine(line);

				if (line.StartsWith("computeData:"))
					break;
			}

			ReadString(compilerPipeStream, unityPipeStream);
		}
	}
}
