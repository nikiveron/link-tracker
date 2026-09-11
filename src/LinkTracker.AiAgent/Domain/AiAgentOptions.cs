using System.ComponentModel.DataAnnotations;

namespace LinkTracker.AiAgent.Domain;

public class AiAgentOptions
{
    public const string SectionName = "AiAgent";

    [Required]
    public FilteringOptions Filtering { get; set; } = new();

    [Range(1, 10000)]
    public int MinLength { get; set; }

    [Required]
    public SummarizationOptions Summarization { get; set; } = new();

    [Required]
    public PrioritizationOptions Prioritization { get; set; } = new();

    [Required]
    public GroupingOptions Grouping { get; set; } = new();
}

public class FilteringOptions
{
    public List<string> StopWords { get; set; } = [];
    public List<string> ExcludedAuthors { get; set; } = [];
}

public class SummarizationOptions
{
    [Range(0, int.MaxValue)]
    public int Threshold { get; set; }
}

public class PrioritizationOptions
{
    public List<string> HighKeywords { get; set; } = [];
    public List<string> LowKeywords { get; set; } = [];
}

public class GroupingOptions
{
    [Range(1, int.MaxValue)]
    public int WindowMs { get; set; } = 30000;
}
