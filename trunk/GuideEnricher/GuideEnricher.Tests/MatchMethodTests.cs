namespace GuideEnricher.Tests
{
    using GuideEnricher.EpisodeMatchMethods;
    using NUnit.Framework;

    [TestFixture]
    public class MatchMethodTests
    {
        [Test]
        public void ReturnsMethodsOrderedByPriority()
        {
            var methods = EpisodeMatchMethodLoader.GetMatchMethods();
            Assert.IsInstanceOf(typeof(EpisodeTitleMatchMethod), methods[0]);
            Assert.IsInstanceOf(typeof(AbsoluteEpisodeNumberMatchMethod), methods[1]);
            Assert.IsInstanceOf(typeof(NoPunctuationMatchMethod), methods[2]);
            Assert.IsInstanceOf(typeof(RemoveCommonWordsMatchMethod), methods[3]);
        }
    }
}