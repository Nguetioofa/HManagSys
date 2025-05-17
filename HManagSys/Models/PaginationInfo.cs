namespace HManagSys.Models
{

    /// <summary>
    /// Informations de pagination
    /// </summary>
    public class PaginationInfo
    {
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public int PreviousPage => HasPreviousPage ? CurrentPage - 1 : 1;
        public int NextPage => HasNextPage ? CurrentPage + 1 : CurrentPage;
    }

}
