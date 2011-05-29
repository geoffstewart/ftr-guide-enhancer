namespace GuideEnricher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using GuideEnricher.EpisodeMatchMethods;

    public class EpisodeMatchMethodLoader
    {
        /// <summary>
        /// Use reflection to add comparison methods for episode names
        /// </summary>
        public static List<IEpisodeMatchMethod> GetMatchMethods()
        {
            var methods = Assembly.GetExecutingAssembly().GetTypes().Where(type => typeof(IEpisodeMatchMethod).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract).ToList();
            methods.Sort(new MatchMethodPriorityComparer());
            
            var matchMethods = new List<IEpisodeMatchMethod>(methods.Count);
            matchMethods.AddRange(methods.Select(method => Activator.CreateInstance(method) as IEpisodeMatchMethod));

            return matchMethods;
        }
    }
}