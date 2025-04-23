using System.Collections.Generic;

namespace HomeCentre.PIM.BAL.Model.QuJs
{
    public class QuJsQuestion
    {
        public string Type { get; set; }

        public string Name { get; set; }

        public IEnumerable<QuJsChoice> Choices { get; set; }

        public string Title { get; set; }

        public bool IsRequired { get; set; } = true;

        public string DefaultValue { get; set; }

        public string Value { get; set; }
    }
}