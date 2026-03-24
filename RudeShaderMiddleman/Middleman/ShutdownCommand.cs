using System;

namespace RudeShaderMiddleman.Middleman
{
	internal partial class CompilerMiddleman
	{
		private void Shutdown()
		{
			middlemanOutputLog.WriteLine($"Shutdown.");

			if (compilerPipeStream.IsConnected)
				compilerPipeStream.Close();

			if (unityPipeStream.IsConnected)
				unityPipeStream.Close();

			middlemanOutputLog.Flush();
			Environment.Exit(0);
		}
	}
}
