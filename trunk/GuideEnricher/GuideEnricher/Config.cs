namespace GuideEnricher
{
    using System.Collections.Generic;
    using System.Configuration;

    public class SeriesNameMap : ConfigurationElement
    {
        // Create the element.
        public SeriesNameMap()
        { }

        // dummy constructor to make collection happy
        public SeriesNameMap(string nothing)
        {
            SchedulesDirectName = nothing;
            TvdbComName = "invalid";
            Ignore = false;
        }

        // Create the element.
        public SeriesNameMap(string sdName,
                             string tvdbName)
        {
            SchedulesDirectName = sdName;
            TvdbComName = tvdbName;
            Ignore = false;
        }

        [ConfigurationProperty("schedulesDirectName",
                               DefaultValue = "",
                               IsRequired = true)]
        public string SchedulesDirectName
        {
            get
            {
                return (string)this["schedulesDirectName"];
            }
            set
            {
                this["schedulesDirectName"] = value;
            }
        }

        [ConfigurationProperty("tvdbComName",
                               DefaultValue = "",
                               IsRequired = true)]
        public string TvdbComName
        {
            get
            {
                return (string)this["tvdbComName"];
            }
            set
            {
                this["tvdbComName"] = value;
            }
        }

        [ConfigurationProperty("ignore",
                               DefaultValue = "false",
                               IsRequired = false)]
        public bool Ignore
        {
            get
            {
                return (bool)this["ignore"];
            }
            set
            {
                this["ignore"] = value;
            }
        }

        protected override bool SerializeElement(
           System.Xml.XmlWriter writer,
           bool serializeCollectionKey)
        {
            bool ret = base.SerializeElement(writer,
                                             serializeCollectionKey);
            // You can enter your custom processing code here.
            return ret;

        }

        protected override bool IsModified()
        {
            bool ret = base.IsModified();
            // You can enter your custom processing code here.
            return ret;
        }
    }

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
