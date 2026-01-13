
namespace Domain
{
    public class RowEntityComparer : IComparer<RowEntity>
    {
        public int Compare(RowEntity x, RowEntity y)
        {
            return x.CompareTo(y);
        }
    }
}
