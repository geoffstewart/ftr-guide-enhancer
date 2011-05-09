namespace GuideEnricher
{
    using System.Collections.Generic;

    public interface IConfiguration
    {
        string getProperty(string key);
        Dictionary<string, string> getSeriesNameMap();
        List<string> getIgnoredSeries();
    }
}