using EasyCompressor;
using K4os.Compression.LZ4.Streams;
using RudeShaderMiddleman.Common.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RudeShaderMiddleman.Common.BlobTable
{
	public class BlobTableFile : IDisposable
	{
		private Stream stream;
		private BlobTableHeader header;

		private byte[] buffer = new byte[1 * 1024 * 1024];
		private int currentSegment = -1;
		private MemoryStream bufferStream;

		public BlobTableFile(Stream stream)
		{
			this.stream = stream;
			bufferStream = new MemoryStream(buffer);

			using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true))
			{
				header = new BlobTableHeader(reader);
			}
		}

		public byte[] GetSegment(int index)
		{
			if (currentSegment == index)
				return buffer;

			int absolutePosition = header.Segments[index].SegmentAbsoluteOffset;
			int compressedLength = header.Segments[index].SegmentCompressedLength;

			stream.Seek(absolutePosition, SeekOrigin.Begin);
			SegmentStream segmentStream = new SegmentStream(stream, compressedLength);

			bufferStream.Seek(0, SeekOrigin.Begin);
			LZMACompressor.Shared.Decompress(segmentStream, bufferStream);

			currentSegment = index;
			return buffer;
		}

		public void Dispose()
		{
			bufferStream.Dispose();
			stream.Dispose();
		}
	}
}
