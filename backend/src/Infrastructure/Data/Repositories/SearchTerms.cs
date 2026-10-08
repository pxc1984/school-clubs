namespace SchoolClubs.Infrastructure.Data.Repositories;

/// <summary>Разбор строки поиска на слова для <c>ILIKE</c>.</summary>
internal static class SearchTerms
{
    private const int MaxTerms = 5;

    /// <summary>Слова запроса как шаблоны <c>%слово%</c> с экранированными <c>%</c>, <c>_</c> и <c>\</c>.</summary>
    public static IReadOnlyList<string> ContainsPatterns(string? query)
    {
        return Words(query).Select(w => $"%{Escape(w)}%").ToList();
    }

    public static IReadOnlyList<string> Words(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        return query.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries).Take(MaxTerms).ToList();
    }

    private static string Escape(string word)
    {
        return word.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
    }
}
