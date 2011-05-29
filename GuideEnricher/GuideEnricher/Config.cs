namespace GuideEnricher
{
    using System.Collections.Generic;
    using System.Configuration;

    public class Config : IConfiguration
    {
        private static Config configInstance;

        private Config()
        {
        }

        public static Config GetInstance()
        {
            if (configInstance == null)
            {
                configInstance = new Config();
            }

            return configInstance;
        }

        public string getProperty(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        public Dictionary<string, string> getSeriesNameMap()
        {
            SeriesNameMapsSection mapSec = ConfigurationManager.GetSection("seriesMapping") as SeriesNameMapsSection;
            if (mapSec == null)
            {
                return new Dictionary<string, string>(0);
            }

            Dictionary<string, string> series = new Dictionary<string, string>(mapSec.SeriesMapping.Count);

            for (int i = 0; i < mapSec.SeriesMapping.Count; i++)
            {
                series.Add(mapSec.SeriesMapping[i].SchedulesDirectName, mapSec.SeriesMapping[i].TvdbComName);
            }

            return series;
        }

        public List<string> getIgnoredSeries()
        {
            SeriesNameMapsSection mapSec = ConfigurationManager.GetSection("seriesMapping") as SeriesNameMapsSection;

            if (mapSec == null)
            {
                return null;
            }

            List<string> l = new List<string>();

            for (int i = 0; i < mapSec.SeriesMapping.Count; i++)
            {
                if (mapSec.SeriesMapping[i].Ignore)
                {
                    l.Add(mapSec.SeriesMapping[i].SchedulesDirectName);
                }
            }
            return l;
        }
    }
}
