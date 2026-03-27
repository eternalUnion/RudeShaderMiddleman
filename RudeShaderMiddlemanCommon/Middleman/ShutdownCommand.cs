using System;

namespace RudeShadermiddleman.Common.Middleman
{
	public partial class CompilerMiddleman
	{
		private void Shutdown()
		{
			middlemanOutputLog.WriteLine($"Shutdown.");

			compilerPipeStream.Dispose();
			unityPipeStream.Dispose();
			middlemanOutputLog.Dispose();

			middlemanOutputLog.Flush();
			Environment.Exit(0);
		}
	}
}
