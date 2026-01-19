namespace Domain
{
    public class RowComparer : IComparer<RowEntity>
    {
        public int Compare(RowEntity x, RowEntity y)
        {
            return x.CompareTo(y);
        }
    }
}
