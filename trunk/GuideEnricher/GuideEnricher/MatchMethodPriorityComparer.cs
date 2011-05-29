namespace GuideEnricher
{
    using System;
    using System.Collections.Generic;
    using GuideEnricher.EpisodeMatchMethods;

    /// <summary>
    /// Uses Priority Attribute on each match method to sort the order
    /// </summary>
    public class MatchMethodPriorityComparer : Comparer<Type>
    {
        public override int Compare(Type x, Type y)
        {
            if (!typeof(IEpisodeMatchMethod).IsAssignableFrom(x))
            {
                throw new Exception("Exception while trying to sort match methods");
            }

            if(!typeof(IEpisodeMatchMethod).IsAssignableFrom(y))
            {
                throw new Exception("Exception while trying to sort match methods");
            }

            var attributeX = Type.GetType(x.FullName).GetCustomAttributes(typeof(MatchMethodPriorityAttribute), false)[0];
            var attributeY = Type.GetType(y.FullName).GetCustomAttributes(typeof(MatchMethodPriorityAttribute), false)[0];
            if(attributeX == null || attributeY == null)
            {
                throw new Exception("Exception while trying to sort match methods");
            }
            
            var priorityX = (attributeX as MatchMethodPriorityAttribute).Priority;
            var priorityY = (attributeY as MatchMethodPriorityAttribute).Priority;

            return priorityX.CompareTo(priorityY);
        }
    }
}