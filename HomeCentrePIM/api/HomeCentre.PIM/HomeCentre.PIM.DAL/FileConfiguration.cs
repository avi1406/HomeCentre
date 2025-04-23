namespace HomeCentre.PIM.DAL;

public class FileConfiguration : Entity
{
    public string Name { get; set; }

    public string FileDirPath { get; set; }

    public string TargetTable { get; set; }
}