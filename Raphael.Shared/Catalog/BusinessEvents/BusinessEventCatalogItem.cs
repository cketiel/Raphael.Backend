namespace Raphael.Shared.Catalog.BusinessEvents;

public sealed class BusinessEventCatalogItem
{
    public string CategoryCode { get; init; } = string.Empty;

    public string CategoryName { get; init; } = string.Empty;

    public string CategoryDescription { get; init; } = string.Empty;


    public string GroupCode { get; init; } = string.Empty;

    public string GroupName { get; init; } = string.Empty;

    public string GroupDescription { get; init; } = string.Empty;


    public string EventCode { get; init; } = string.Empty;

    public string EventName { get; init; } = string.Empty;

    public string EventDescription { get; init; } = string.Empty;


    public string Source { get; init; } = string.Empty;
}