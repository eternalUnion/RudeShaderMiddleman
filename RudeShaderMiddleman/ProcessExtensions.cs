using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RudeShaderMiddleman
{
	internal static class ProcessExtensions
	{
		public static Task WaitForExitAsync(this Process process, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (process.HasExited) return Task.CompletedTask;

			var tcs = new TaskCompletionSource<object>();
			process.EnableRaisingEvents = true;
			process.Exited += (sender, args) => tcs.TrySetResult(null);
			if (cancellationToken != default(CancellationToken))
				cancellationToken.Register(() => tcs.SetCanceled());

			return process.HasExited ? Task.CompletedTask : tcs.Task;
		}
	}
}
