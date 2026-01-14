
namespace Domain
{
    public readonly struct RowEntity(long number, string text) : IComparable<RowEntity>
    {
        public long Number { get; } = number;

        public string Text { get; } = text;

        private const short MinRowMemory = 64;

        public int CompareTo(RowEntity other)
        {
            int comparisonResult = string.Compare(Text, other.Text, StringComparison.Ordinal); // compare by string part (alphabetically)

            if (comparisonResult != 0)
            { 
                return comparisonResult;
            } 
            
            return Number.CompareTo(other.Number); // compare by number part (ascending)
        }

        public override string ToString() => $"{Number}{FileGeneratingService.FileRowSeparator}{Text}";

        public static RowEntity GetRowFromLine(string line)
        {
            const string separatorValue = FileGeneratingService.FileRowSeparator;
            int separatorIndex = line.IndexOf(separatorValue);

            if (separatorIndex == -1)
            {
                return new RowEntity(0, line); // to handle rows without correct separator
            }

            ReadOnlySpan<char> numSpan = line.AsSpan(0, separatorIndex);
            long number = long.Parse(numSpan);

            int textIndex = separatorIndex + separatorValue.Length;
            bool isWithText = textIndex < line.Length;
            string text = isWithText ? line[textIndex..] : string.Empty;

            return new RowEntity(number, text);
        }

        public static int GetRowBytes(int rowTextLength)
        {
            return (rowTextLength * 2) + sizeof(long) + MinRowMemory;
        }

        public int GetRowBytes()
        {
            return GetRowBytes(Text.Length);
        }
    }
}
