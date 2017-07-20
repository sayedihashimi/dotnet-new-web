namespace TemplatesShared
{
    public interface ITemplateSearchResult
    {
        bool IsMatch { get; set; }
        int SearchValue { get; set; }
    }
}