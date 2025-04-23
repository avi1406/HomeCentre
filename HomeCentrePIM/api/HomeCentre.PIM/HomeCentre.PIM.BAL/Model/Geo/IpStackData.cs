using System;
using System.Collections.Generic;

namespace HomeCentre.PIM.BAL.Model.Geo
{
    public class Language
    {
        public string Code { get; set; }

        public string Name { get; set; }

        public string Native { get; set; }
    }

    public class Location
    {
        public int GeonameId { get; set; }

        public string Capital { get; set; }

        public List<Language> Languages { get; set; }

        public string CountryFlag { get; set; }

        public string CountryFlagEmoji { get; set; }

        public string CountryFlagEmojiUnicode { get; set; }

        public string CallingCode { get; set; }

        public bool IsEu { get; set; }
    }

    public class TimeZone
    {
        public string Id { get; set; }

        public DateTime CurrentTime { get; set; }

        public int GmtOffset { get; set; }

        public string Code { get; set; }

        public bool IsDaylightSaving { get; set; }
    }

    public class Currency
    {
        public string Code { get; set; }

        public string Name { get; set; }

        public string Plural { get; set; }

        public string Symbol { get; set; }

        public string SymbolNative { get; set; }
    }

    public class Connection
    {
        public int Asn { get; set; }

        public string Isp { get; set; }
    }

    public class Security
    {
        public bool IsProxy { get; set; }

        public object ProxyType { get; set; }

        public bool IsCrawler { get; set; }

        public object CrawlerName { get; set; }

        public object CrawlerType { get; set; }

        public bool IsTor { get; set; }

        public string ThreatLevel { get; set; }

        public object ThreatTypes { get; set; }
    }

    public class IpStackData
    {
        public string Ip { get; set; }

        public string Hostname { get; set; }

        public string Type { get; set; }

        public string ContinentCode { get; set; }

        public string ContinentName { get; set; }

        public string CountryCode { get; set; }

        public string CountryName { get; set; }

        public string RegionCode { get; set; }

        public string RegionName { get; set; }

        public string City { get; set; }

        public string Zip { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public Location Location { get; set; }

        public TimeZone TimeZone { get; set; }

        public Currency Currency { get; set; }

        public Connection Connection { get; set; }

        public Security Security { get; set; }
    }
}