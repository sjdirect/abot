
namespace Abot.SiteSimulator.Models
{
    public class PageSpecs
    {
        public int Status200Count { get; set; }

        public int Status403Count { get; set; }

        public int Status404Count { get; set; }

        public int Status500Count { get; set; }

        public int Status503Count { get; set; }

        public int Status200StartingIndex { get; set; }

        public int Status403StartingIndex { get; set; }

        public int Status404StartingIndex { get; set; }

        public int Status500StartingIndex { get; set; }

        public int Status503StartingIndex { get; set; }
    }
}