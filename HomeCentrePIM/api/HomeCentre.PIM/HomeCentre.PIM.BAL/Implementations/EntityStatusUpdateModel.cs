using HomeCentre.PIM.DAL;

namespace HomeCentre.PIM.BAL.Implementations
{
    public class EntityStatusUpdateModel<T>
        where T : Entity
    {
        public long Id { get; set; }

        public bool IsActive { get; set; }
    }
}