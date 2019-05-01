using System;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Twilio.Types;
using Twilio.Rest.Api.V2010.Account;

using AzureSkyMedia.PlatformServices;

namespace AzureSkyMedia.FunctionApp
{
    public static class MediaWorkflowJobStateFinal
    {
        [FunctionName("MediaWorkflow-JobStateFinal")]
        [return: TwilioSms(AccountSidSetting = "Twilio.AccountId", AuthTokenSetting = "Twilio.AccountToken", From = "%Twilio.FromNumber%")]
        public static CreateMessageOptions Run([EventGridTrigger] EventGridEvent eventTrigger, ILogger logger)
        {
            CreateMessageOptions twilioMessage = null;
            try
            {
                logger.LogInformation(JsonConvert.SerializeObject(eventTrigger, Formatting.Indented));
                MediaPublishNotification publishNotification = MediaClient.PublishJobOutput(eventTrigger);
                if (!string.IsNullOrEmpty(publishNotification.StreamingUrl))
                {
                    logger.LogInformation(publishNotification.StreamingUrl);
                }
                if (!string.IsNullOrEmpty(publishNotification.StatusMessage))
                {
                    logger.LogInformation(publishNotification.StatusMessage);
                    if (!string.IsNullOrEmpty(publishNotification.PhoneNumber))
                    {
                        PhoneNumber twilioPhoneNumber = new PhoneNumber(publishNotification.PhoneNumber);
                        twilioMessage = new CreateMessageOptions(twilioPhoneNumber)
                        {
                            Body = publishNotification.StatusMessage
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
            return twilioMessage;
        }
    }
}