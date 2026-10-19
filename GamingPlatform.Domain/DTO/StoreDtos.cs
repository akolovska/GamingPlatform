namespace GamingPlatform.Domain.DTO
{
    public enum StoreSort { Name, PriceLow, PriceHigh, Newest, TopRated, Popular }

    // The store page's search box, filters and sorting (bound from the query string)
    public class StoreQueryDto
    {
        public string? Q { get; set; }
        public Guid? GenreId { get; set; }
        public bool Free { get; set; }
        public bool OnSale { get; set; }
        public bool ComingSoon { get; set; }
        public StoreSort Sort { get; set; } = StoreSort.Name;
        public int Page { get; set; } = 1;

        public bool IsFiltered => !string.IsNullOrWhiteSpace(Q) || GenreId != null || Free || OnSale || ComingSoon;
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize == 0 ? 0 : (TotalCount + PageSize - 1) / PageSize;
    }

    // "Because you played X": games like one you own
    public class RecommendationDto
    {
        public Guid BasedOnId { get; set; }
        public string BasedOnTitle { get; set; } = string.Empty;
        public List<GameDto> Games { get; set; } = new();
    }
}
