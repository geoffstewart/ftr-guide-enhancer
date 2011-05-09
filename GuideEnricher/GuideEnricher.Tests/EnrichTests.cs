namespace GuideEnricher.Tests
{
    using ForTheRecord.Entities;
    using NUnit.Framework;

    [TestFixture]
    public class EnrichTests
    {
        [Test]
        public void CanEnrichAProgram()
        {
            var program = new GuideProgram();
            program.Title = "The Big Bang Theory";
            program.SubTitle = "(12)";
        }
    }
}
