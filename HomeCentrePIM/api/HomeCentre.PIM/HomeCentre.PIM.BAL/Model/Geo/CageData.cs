using System.Collections.Generic;
using Newtonsoft.Json;

namespace HomeCentre.PIM.BAL.Model.Geo
{
    public class License
    {
        public string Name { get; set; }

        public string Url { get; set; }
    }

    public class Rate
    {
        public int Limit { get; set; }

        public int Remaining { get; set; }

        public int Reset { get; set; }
    }

    public class Northeast
    {
        public double Lat { get; set; }

        public double Lng { get; set; }
    }

    public class Southwest
    {
        public double Lat { get; set; }

        public double Lng { get; set; }
    }

    public class Bounds
    {
        public Northeast Northeast { get; set; }

        public Southwest Southwest { get; set; }
    }

    public class Components
    {
        [JsonProperty("ISO_3166-1_alpha-2")]
        public string Iso31661Alpha2 { get; set; }

        [JsonProperty("ISO_3166-1_alpha-3")]
        public string Iso31661Alpha3 { get; set; }

        public string Category { get; set; }

        public string Type { get; set; }

        public string City { get; set; }

        public string CityDistrict { get; set; }

        public string Continent { get; set; }

        public string Country { get; set; }

        public string CountryCode { get; set; }

        public string County { get; set; }

        public string Hospital { get; set; }

        public string Postcode { get; set; }

        public string Road { get; set; }

        public string State { get; set; }

        public string StateCode { get; set; }

        public string StateDistrict { get; set; }

        public string Suburb { get; set; }
    }

    public class Geometry
    {
        public double Lat { get; set; }

        public double Lng { get; set; }
    }

    public class Result
    {
        public Bounds Bounds { get; set; }

        public Components Components { get; set; }

        public int Confidence { get; set; }

        public string Formatted { get; set; }

        public Geometry Geometry { get; set; }
    }

    public class Status
    {
        public int Code { get; set; }

        public string Message { get; set; }
    }

    public class StayInformed
    {
        public string Blog { get; set; }

        public string Twitter { get; set; }
    }

    public class Timestamp
    {
        public string CreatedHttp { get; set; }

        public int CreatedUnix { get; set; }
    }

    public class CageData
    {
        public string Documentation { get; set; }

        public List<License> Licenses { get; set; }

        public Rate Rate { get; set; }

        public List<Result> Results { get; set; }

        public Status Status { get; set; }

        public StayInformed StayInformed { get; set; }

        public string Thanks { get; set; }

        public Timestamp Timestamp { get; set; }

        public int TotalResults { get; set; }
    }
}