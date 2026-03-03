namespace Domain
{
    public class RowComparer : IComparer<RowValueObject>
    {
        public int Compare(RowValueObject x, RowValueObject y)
        {
            return x.CompareTo(y);
        }
    }
}
