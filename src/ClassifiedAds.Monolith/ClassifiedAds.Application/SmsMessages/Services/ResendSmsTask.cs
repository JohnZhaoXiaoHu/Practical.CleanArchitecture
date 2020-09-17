using ClassifiedAds.Application.SmsMessages.DTOs;
using ClassifiedAds.Domain.Entities;
using ClassifiedAds.Domain.Infrastructure.MessageBrokers;
using ClassifiedAds.Domain.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace ClassifiedAds.Application.SmsMessages.Services
{
    public class ResendSmsTask
    {
        private readonly ILogger _logger;
        private readonly IRepository<SmsMessage, Guid> _repository;
        private readonly IMessageSender<SmsMessageCreatedEvent> _smsMessageCreatedEventSender;

        public ResendSmsTask(ILogger<ResendSmsTask> logger,
            IRepository<SmsMessage, Guid> repository,
            IMessageSender<SmsMessageCreatedEvent> smsMessageCreatedEventSender)
        {
            _logger = logger;
            _repository = repository;
            _smsMessageCreatedEventSender = smsMessageCreatedEventSender;
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
                foreach (var sms in messages)
                {
                    _smsMessageCreatedEventSender.Send(new SmsMessageCreatedEvent { Id = sms.Id });

                    sms.RetriedCount++;

                    _repository.AddOrUpdate(sms);
                    _repository.UnitOfWork.SaveChanges();
                }
            }
            else
            {
                _logger.LogInformation("No SMS to resend.");
            }

            return messages.Count;
        }
    }
}
