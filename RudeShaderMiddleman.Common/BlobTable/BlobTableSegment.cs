using System;
using System.Collections.Generic;
using System.IO;

namespace RudeShaderMiddleman.Common.BlobTable
{
	public class BlobTableSegment
	{
		public int SegmentAbsoluteOffset;
		public int SegmentCompressedLength;

		public BlobTableSegment(BinaryReader reader)
		{
			SegmentAbsoluteOffset = reader.ReadInt32();
			SegmentCompressedLength = reader.ReadInt32();
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(SegmentAbsoluteOffset);
			writer.Write(SegmentCompressedLength);
		}
	}
}
