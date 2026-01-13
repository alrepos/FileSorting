
namespace Domain
{
    public readonly struct RowEntity : IComparable<RowEntity>
    {
        public long Number { get; }

        public string Text { get; }

        public RowEntity(long number, string text)
        {
            Number = number;
            Text = text;
        }

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
