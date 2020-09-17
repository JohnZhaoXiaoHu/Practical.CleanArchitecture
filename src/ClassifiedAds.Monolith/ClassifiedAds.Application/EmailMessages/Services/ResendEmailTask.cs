using ClassifiedAds.Application.EmailMessages.DTOs;
using ClassifiedAds.Domain.Entities;
using ClassifiedAds.Domain.Infrastructure.MessageBrokers;
using ClassifiedAds.Domain.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace ClassifiedAds.Application.EmailMessages.Services
{
    public class ResendEmailTask
    {
        private readonly ILogger _logger;
        private readonly IRepository<EmailMessage, Guid> _repository;
        private readonly IMessageSender<EmailMessageCreatedEvent> _emailMessageCreatedEventSender;

        public ResendEmailTask(ILogger<ResendEmailTask> logger,
            IRepository<EmailMessage, Guid> repository,
            IMessageSender<EmailMessageCreatedEvent> emailMessageCreatedEventSender)
        {
            _logger = logger;
            _repository = repository;
            _emailMessageCreatedEventSender = emailMessageCreatedEventSender;
        }

        public int Run()
        {
            var dateTime = DateTimeOffset.Now.AddMinutes(-1);

            var messages = _repository.GetAll()
                .Where(x => x.SentDateTime == null && x.RetriedCount < 3)
                .Where(x => (x.RetriedCount == 0 && x.CreatedDateTime < dateTime) || (x.RetriedCount != 0 && x.UpdatedDateTime < dateTime))
                .ToList();

            if (messages.Any())
            {
                foreach (var email in messages)
                {
                    _emailMessageCreatedEventSender.Send(new EmailMessageCreatedEvent { Id = email.Id });

                    email.RetriedCount++;

                    _repository.AddOrUpdate(email);
                    _repository.UnitOfWork.SaveChanges();
                }
            }
            else
            {
                _logger.LogInformation("No email to resend.");
            }

            return messages.Count;
        }
    }
}
