namespace HomeCentre.PIM.Common.Interfaces
{
    public interface IJsonSerializer
    {
        string Serialize<T>(T t);

        T Deserialize<T>(string s);
    }
}