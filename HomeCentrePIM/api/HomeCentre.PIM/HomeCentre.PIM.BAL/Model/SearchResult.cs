using System.Collections.Generic;
using HomeCentre.PIM.DAL;

namespace HomeCentre.PIM.BAL.Model
{
    public class SearchResult<T>
    {
        private readonly object totalCount;
        private List<Marketplace> items;

        public long TotalNoOfRecords { get; set; }

        public List<T> Records { get; set; }

        public List<Marketplace> Results { get; set; }
    }
}