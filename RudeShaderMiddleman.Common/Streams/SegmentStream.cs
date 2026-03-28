using System;
using System.Collections.Generic;
using System.IO;

namespace RudeShaderMiddleman.Common.Streams
{
	internal class SegmentStream : Stream
	{
		private Stream baseStream;
		private long startPos;
		private long endPos;

		public SegmentStream(Stream baseStream, long length)
		{
			this.baseStream = baseStream;
			startPos = baseStream.Position;
			endPos = baseStream.Position + length;
		}

		public override bool CanRead => baseStream.CanRead;

		public override bool CanSeek => baseStream.CanSeek;

		public override bool CanWrite => baseStream.CanWrite;

		public override long Length => endPos - startPos;

		public override long Position { get => baseStream.Position - startPos; set
			{
				long newPos = startPos + value;
				if (newPos > endPos)
					newPos = endPos;

				baseStream.Position = newPos;
			}
		}

		public override void Flush()
		{
			baseStream.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return baseStream.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			switch (origin)
			{
				case SeekOrigin.Begin:
					return baseStream.Seek(Math.Min(startPos + offset, endPos), origin);

				case SeekOrigin.Current:
					return baseStream.Seek(Math.Min(baseStream.Position + offset, endPos), origin);

				case SeekOrigin.End:
					return baseStream.Seek(Math.Max(startPos, endPos - offset - 1), origin);

				default:
					throw new ArgumentException("Seek origin is invalid");
			}
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			baseStream.Write(buffer, offset, count);
		}
	}
}
