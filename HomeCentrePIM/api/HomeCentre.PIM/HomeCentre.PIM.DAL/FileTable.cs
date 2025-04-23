namespace HomeCentre.PIM.DAL
{
    public class FileTable : Entity
    {
        public string Name { get; set; }

        public string Oid { get; set; }

        public string Type { get; set; }

        public long ContentLength { get; set; }

        public string UniqueId { get; set; }
    }
}