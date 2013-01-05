namespace GuideEnricher.Tests
{
    using System;
    using System.Collections.Generic;

    using ArgusTV.DataContracts;
    using GuideEnricher.Model;
    using NUnit.Framework;
    using Should;

    [TestFixture]
    public class MatchMethodTests
    {
        [Test]
        public void ShouldUpdateSimilarPrograms()
        {
            var programA =
                new GuideEnricherEntities(
                    new GuideProgram
                        {
                            Title = "Intervention",
                            SubTitle = "Sarah; Mikeal",
                            PreviouslyAiredTime = new DateTime(2011, 6, 27),
                            GuideProgramId = Guid.NewGuid()
                        });

            var programB =
                new GuideEnricherEntities(
                    new GuideProgram
                        {
                            Title = "Intervention",
                            SubTitle = "Sarah; Mikeal",
                            GuideProgramId = Guid.NewGuid()
                        });

            var matchedSeries = new GuideEnricherSeries("Intervention", false, false, false);
            var pendingSeries = new List<GuideEnricherEntities>(1) { programB };
            
            matchedSeries.AddProgram(programA);



            var similarPrograms = matchedSeries.FindSimilarPrograms(pendingSeries, programA );

            similarPrograms.Count.ShouldEqual(1);
        }
    }
}