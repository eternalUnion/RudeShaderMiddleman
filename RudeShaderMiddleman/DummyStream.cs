using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RudeShaderMiddleman
{
	internal class DummyStream : Stream
	{
		private long _pos = 0;

		public override bool CanRead => true;

		public override bool CanSeek => true;

		public override bool CanWrite => true;

		public override long Length => _pos + 1;

		public override long Position { get => _pos; set => _pos = value; }

		public override void Flush()
		{
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return 0;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			_pos = offset;
			return _pos;
		}

		public override void SetLength(long value)
		{
			_pos = Math.Max(0, value - 1);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			_pos += count;
		}
	}
}
