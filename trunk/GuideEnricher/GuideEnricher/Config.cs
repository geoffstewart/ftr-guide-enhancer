using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;

#region
//*** Auxiliary Classes ***//
namespace GuideEnricher {
   public class SeriesNameMap : ConfigurationElement
   {
      // Create the element.
      public SeriesNameMap()
      { }
      
      // dummy constructor to make collection happy
      public SeriesNameMap(string nothing) {
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
      
      protected override void DeserializeElement(
         System.Xml.XmlReader reader,
         bool serializeCollectionKey)
      {
         base.DeserializeElement(reader,
                                 serializeCollectionKey);
         // You can your custom processing code here.
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

   [ConfigurationCollection(typeof(SeriesNameMap), AddItemName = "seriesMap", CollectionType = ConfigurationElementCollectionType.BasicMap)]
   public class SeriesNameMapCollection : ConfigurationElementCollection {
      protected override ConfigurationElement CreateNewElement()
      {
         return new SeriesNameMap();
      }

      protected override object GetElementKey( ConfigurationElement element )
      {
         return ( (SeriesNameMap) element ).SchedulesDirectName;
      }

      public void Add( SeriesNameMap element )
      {
         BaseAdd( element );
      }

      public void Clear()
      {
         BaseClear();
      }

      public int IndexOf( SeriesNameMap element )
      {
         return BaseIndexOf( element );
      }

      public void Remove( SeriesNameMap element )
      {
         if( BaseIndexOf( element ) >= 0 )
         {
            BaseRemove( element.SchedulesDirectName );
         }
      }

      public void RemoveAt( int index )
      {
         BaseRemoveAt( index );
      }

      public SeriesNameMap this[ int index ]
      {
         get { return (SeriesNameMap) BaseGet( index ); }
         set
         {
            if( BaseGet( index ) != null )
            {
               BaseRemoveAt( index );
            }
            BaseAdd( index, value );
         }
      }

   }


   public class SeriesNameMapsSection : ConfigurationSection {
      private static readonly ConfigurationProperty _propSeriesMap = new ConfigurationProperty(
         null,
         typeof(SeriesNameMapCollection),
         null,
         ConfigurationPropertyOptions.IsDefaultCollection
        );

      private static ConfigurationPropertyCollection _properties = new ConfigurationPropertyCollection();

      static SeriesNameMapsSection()
      {
         _properties.Add( _propSeriesMap );
      }

      [ConfigurationProperty( "", Options = ConfigurationPropertyOptions.IsDefaultCollection )]
      public SeriesNameMapCollection SeriesMapping
      {
         get { return (SeriesNameMapCollection) base[ _propSeriesMap ]; }
      }

   }

   public class Config {
      
      public static string getProperty(string key) {
         return ConfigurationManager.AppSettings[key];
         
      }
      
      public static Hashtable getSeriesNameMap() {
         SeriesNameMapsSection mapSec = ConfigurationManager.GetSection("seriesMapping") as SeriesNameMapsSection;
         
         Hashtable map = new Hashtable();
         
         for (int i = 0; i < mapSec.SeriesMapping.Count; i++) {
            map.Add(mapSec.SeriesMapping[i].SchedulesDirectName,
                    mapSec.SeriesMapping[i].TvdbComName);
            
         }
         return map;
      }
      
      public static List<string> getIgnoredSeries() {
         SeriesNameMapsSection mapSec = ConfigurationManager.GetSection("seriesMapping") as SeriesNameMapsSection;
         
         List<string> l = new List<string>();
         
         for (int i = 0; i < mapSec.SeriesMapping.Count; i++) {
            if (mapSec.SeriesMapping[i].Ignore) {
               l.Add(mapSec.SeriesMapping[i].SchedulesDirectName);
            }
         }
         return l;
      }
      
   }
}
#endregion