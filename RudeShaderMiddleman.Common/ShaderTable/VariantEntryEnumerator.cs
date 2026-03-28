using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RudeShaderMiddleman.Common.ShaderTable
{
	public class VariantEntryEnumerator : IEnumerable<VariantEntry>, IEnumerator<VariantEntry>
	{
		private readonly BinaryReader reader;
		private readonly List<string> nameMap;
		
		public readonly int TotalNumberOfVariants;
		private int CurrentEntryNumber = -1;

		public VariantEntryEnumerator(BinaryReader reader, List<string> nameMap)
		{
			this.reader = reader;
			this.nameMap = nameMap;
			TotalNumberOfVariants = reader.ReadInt32();
		}

		private VariantEntry currentEntry = null;
		public VariantEntry Current => currentEntry;

		object IEnumerator.Current => Current;

		public void Dispose()
		{
			reader.Dispose();
		}

		public IEnumerator<VariantEntry> GetEnumerator()
		{
			return this;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public bool MoveNext()
		{
			CurrentEntryNumber += 1;
			if (CurrentEntryNumber >= TotalNumberOfVariants)
				return false;

			currentEntry = new VariantEntry(reader, nameMap);
			return true;
		}

		public void Reset()
		{
			throw new NotImplementedException();
		}
	}
}
