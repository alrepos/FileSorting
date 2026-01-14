
namespace Domain
{
    public readonly struct RowEntity(long number, string text) : IComparable<RowEntity>
    {
        public long Number { get; } = number;

        public string Text { get; } = text;

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
    }
}
