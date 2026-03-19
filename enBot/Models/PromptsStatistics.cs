using System;
using System.Collections.Generic;

namespace enBot.Models;

public class PromptsStatistics : PromptsStatisticsBase
{
    public List<DayPromptsStatistics> DailyStatistics { get; set; } = [];
}

public class DayPromptsStatistics: PromptsStatisticsBase
{
    public DateTime Date { get; set; }
}

public abstract class PromptsStatisticsBase
{
    public int TotalPrompts { get; set; }
    public double AvgWeightedScore { get; set; }
    public double AvgWeightedComplexity { get; set; }
}