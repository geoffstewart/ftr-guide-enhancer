namespace GuideEnricher.Model
{
    using System.Collections.Generic;

    public class GuideProgramEqualityComparer : IEqualityComparer<GuideEnricherEntities>
    {
        public bool Equals(GuideEnricherEntities x, GuideEnricherEntities y)
        {
            return x.GuideProgramId == y.GuideProgramId;
        }

        public int GetHashCode(GuideEnricherEntities obj)
        {
            return obj.GetHashCode();
        }
    }
}