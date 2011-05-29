namespace GuideEnricher
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class MatchMethodPriorityAttribute : Attribute
    {
        public int Priority { get; set; }
    }
}
