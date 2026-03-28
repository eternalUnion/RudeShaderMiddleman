using System;
using System.Collections.Generic;
using System.IO;

namespace RudeShaderMiddleman.Common.BlobTable
{
	public class BlobTableHeader
	{
		public List<BlobTableSegment> Segments = new List<BlobTableSegment>();

		public BlobTableHeader(BinaryReader reader)
		{
			int cnt = reader.ReadInt32();
			for (int i = 0; i < cnt; i++)
			{
				Segments.Add(new BlobTableSegment(reader));
			}
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Segments.Count);
			foreach (BlobTableSegment segment in Segments)
			{
				segment.Serialize(writer);
			}
		}
	}
}
