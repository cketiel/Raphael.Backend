using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.DTOs.Catalog
{
    /// <summary>One of the groups the catalog is divided into.</summary>
    public class CatalogCategoryDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string NameEn { get; set; }
        public string NameEs { get; set; }
        public int DisplayOrder { get; set; }

        /// <summary>How many rows of the catalog fall in this group right now.</summary>
        public int Count { get; set; }
    }

    public class CountyDto
    {
        public int Id { get; set; }
        public string State { get; set; }
        public string Name { get; set; }
    }

    /// <summary>A page of results, and how many there were in total.</summary>
    public class CatalogPageDto<T>
    {
        public IReadOnlyList<T> Items { get; set; } = new List<T>();

        /// <summary>Rows matching the search, not rows in this page.</summary>
        public int TotalCount { get; set; }

        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        /// <summary>
        /// What the search could be narrowed by, each with its count. Empty when the caller did
        /// not ask for facets: they cost a query each.
        /// </summary>
        public CatalogFacetsDto? Facets { get; set; }

        /// <summary>
        /// Filled only when the search returned nothing: near-matches on the name, so a typo
        /// does not read as "we do not have it".
        /// </summary>
        public IReadOnlyList<CatalogSuggestionDto> Suggestions { get; set; } = new List<CatalogSuggestionDto>();
    }

    public class CatalogFacetsDto
    {
        public IReadOnlyList<CatalogFacetValueDto> Categories { get; set; } = new List<CatalogFacetValueDto>();
        public IReadOnlyList<CatalogFacetValueDto> Counties { get; set; } = new List<CatalogFacetValueDto>();
        public IReadOnlyList<CatalogFacetValueDto> Cities { get; set; } = new List<CatalogFacetValueDto>();

        /// <summary>How many of the matching rows this company already has, and how many it does not.</summary>
        public int InMyCompanyCount { get; set; }
        public int NotInMyCompanyCount { get; set; }
    }

    public class CatalogFacetValueDto
    {
        public int? Id { get; set; }
        public string Label { get; set; }
        public int Count { get; set; }
    }

    /// <summary>A name close enough to what was typed to be worth offering.</summary>
    public class CatalogSuggestionDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? City { get; set; }
    }

    /// <summary>What the catalog screen sends when the user types, filters or turns a page.</summary>
    public class CatalogSearchRequestDto
    {
        /// <summary>
        /// What the user typed. Split on whitespace; every term has to match something. All
        /// digits means a phone number.
        /// </summary>
        [MaxLength(200)]
        public string? Term { get; set; }

        public int? CategoryId { get; set; }
        public int? CountyId { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(2)]
        public string? State { get; set; }

        /// <summary>
        /// <c>true</c> for what this company already added, <c>false</c> for what it has not,
        /// <c>null</c> for both. "This company" is the caller's provider, the general broker
        /// being the one with no provider at all.
        /// </summary>
        public bool? InMyCompany { get; set; }

        public bool? HasPhone { get; set; }
        public bool? HasEmail { get; set; }
        public bool? HasWebsite { get; set; }

        /// <summary>Retired rows are out unless asked for. They are never deleted.</summary>
        public bool IncludeInactive { get; set; }

        /// <summary>Facets cost a query each, so the screen asks for them only when it shows them.</summary>
        public bool IncludeFacets { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 50;

        public CatalogSortBy SortBy { get; set; } = CatalogSortBy.Relevance;
    }

    public enum CatalogSortBy
    {
        /// <summary>Best match first. With no search term this falls back to name.</summary>
        Relevance = 0,
        Name = 1,
        City = 2,
        /// <summary>Most recently added to the catalog first.</summary>
        Newest = 3
    }

    /// <summary>One row of a file being imported, already mapped to our field names.</summary>
    /// <remarks>
    /// The client reads the spreadsheet and maps the columns; the server never sees the file.
    /// <see cref="RowNumber"/> is the row in the original so a rejection can be pointed at.
    /// </remarks>
    public class CatalogImportRequestDto<T>
    {
        /// <summary>Ties every batch of one run together, for tracing and for undoing a bad import.</summary>
        public Guid ImportBatchId { get; set; }

        [MaxLength(260)]
        public string? SourceFile { get; set; }

        public int CategoryId { get; set; }

        public List<T> Rows { get; set; } = new();
    }

    /// <summary>What happened to each row, one entry per row sent.</summary>
    public class CatalogImportResultDto
    {
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Failed { get; set; }

        public List<CatalogImportRowResultDto> Rows { get; set; } = new();
    }

    public class CatalogImportRowResultDto
    {
        public int RowNumber { get; set; }

        /// <summary>The catalog row it became, when it became one.</summary>
        public int? Id { get; set; }

        public CatalogImportOutcome Outcome { get; set; }

        /// <summary>
        /// A code, not a sentence: the client owns the wording and the translation.
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>The field the problem is in, when it is in one.</summary>
        public string? Field { get; set; }
    }

    public enum CatalogImportOutcome
    {
        Created = 0,
        Updated = 1,
        Rejected = 2
    }

    /// <summary>Rejection codes. The client turns these into words, in the user's language.</summary>
    public static class CatalogImportErrorCodes
    {
        public const string NameMissing = "NAME_MISSING";
        public const string NameTooLong = "NAME_TOO_LONG";
        public const string CategoryUnknown = "CATEGORY_UNKNOWN";
        public const string DuplicateInBatch = "DUPLICATE_IN_BATCH";
        public const string FieldTooLong = "FIELD_TOO_LONG";
        public const string SaveFailed = "SAVE_FAILED";
    }

    /// <summary>Asks for coordinates for rows that have none.</summary>
    public class CatalogGeocodeRequestDto
    {
        public List<int> Ids { get; set; } = new();
    }

    public class CatalogGeocodeResultDto
    {
        public int Resolved { get; set; }
        public int NotFound { get; set; }
        public int Failed { get; set; }

        /// <summary>Rows that already had coordinates and were left alone.</summary>
        public int Skipped { get; set; }

        /// <summary>
        /// Calls left against today's Geocoding quota, as far as we can tell. The screen shows
        /// this because the quota is 1,500 a day and the catalog is thousands of rows.
        /// </summary>
        public int? RemainingDailyQuota { get; set; }
    }
}
