using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentEmail.Core;
using FluentEmail.Core.Models;
using HomeCentre.PIM.BAL.Contracts;
using Microsoft.Extensions.Hosting;

namespace HomeCentre.PIM.BAL
{
    public class SmtpMailEngine : IMailEngine
    {
        private readonly IHostEnvironment _environment;
        private readonly IFluentEmailFactory _email;

        public SmtpMailEngine(IHostEnvironment environment, IFluentEmailFactory email)
        {
            _environment = environment;
            _email = email;
        }

        public async Task SendEmail(string templateName, object model, string subject, string fromEmail,
            string[] toEmails,
            string[] ccEmails = null, string[] bccEmails = null)
        {
            ccEmails ??= new string[] { };
            bccEmails ??= new string[] { };
            var templatePath = Path.Combine(_environment.ContentRootPath,
                $"Templates/{templateName}.cshtml");
            fromEmail ??= "kayviz.demo@gmail.com";
            if (!toEmails.Any() && !ccEmails.Any() && !bccEmails.Any())
            {
                return;
            }

            var response = await _email

                // .From(fromEmail)
                .Create()
                .To(toEmails.Distinct().Where(x => new EmailAddressAttribute().IsValid(x)).Select(x => new Address(x))
                    .ToList())
                .CC(ccEmails.Distinct().Where(x => new EmailAddressAttribute().IsValid(x)).Select(x => new Address(x))
                    .ToList())
                .BCC(bccEmails.Distinct().Where(x => new EmailAddressAttribute().IsValid(x)).Select(x => new Address(x))
                    .ToList())
                .Subject(subject)

                // .UsingTemplateFromEmbedded(templatePath,model,typeof(MailAction).Assembly)
                .UsingTemplateFromFile(templatePath, model)
                .SendAsync();

            if (response.ErrorMessages.Count > 0)
            {
                throw new Exception($"Error while sending email, : {string.Join(",", response.ErrorMessages)}");
            }
        }
    }
}