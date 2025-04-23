using System.Collections.Generic;

namespace HomeCentre.PIM.BAL.Model.QuJs
{
    public class Page
    {
        public string Name { get; set; }

        public List<QuJsQuestion> Elements { get; set; }

        public string Title { get; set; }
    }

    public class QuJs
    {
        public List<Page> Pages { get; set; }
    }
}