using System.Threading.Tasks;

namespace HomeCentre.PIM.BAL.Contracts
{
    public interface IMailEngine
    {
        Task SendEmail(string templateName, object model, string subject, string fromEmail,
            string[] toEmails,
            string[] ccEmails = null, string[] bccEmails = null);
    }
}