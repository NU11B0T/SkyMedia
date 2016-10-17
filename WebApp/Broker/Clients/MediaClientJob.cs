﻿using System.Collections.Generic;

using Microsoft.WindowsAzure.MediaServices.Client;

using SkyMedia.WebApp.Models;

namespace SkyMedia.ServiceBroker
{
    internal partial class MediaClient
    {
        private string GetProcessorId(MediaProcessor mediaProcessor)
        {
            string processorId = null;
            switch (mediaProcessor)
            {
                case MediaProcessor.EncoderStandard:
                    string settingKey = Constants.AppSettings.MediaProcessorEncoderStandardId;
                    processorId = AppSetting.GetValue(settingKey);
                    break;
                case MediaProcessor.EncoderPremium:
                    settingKey = Constants.AppSettings.MediaProcessorEncoderPremiumId;
                    processorId = AppSetting.GetValue(settingKey);
                    break;
                case MediaProcessor.IndexerV1:
                    settingKey = Constants.AppSettings.MediaProcessorIndexerV1Id;
                    processorId = AppSetting.GetValue(settingKey);
                    break;
                case MediaProcessor.IndexerV2:
                    settingKey = Constants.AppSettings.MediaProcessorIndexerV2Id;
                    processorId = AppSetting.GetValue(settingKey);
                    break;
                case MediaProcessor.VideoSummarization:
                    settingKey = Constants.AppSettings.MediaProcessorVideoSummarizationId;
                    processorId = AppSetting.GetValue(settingKey);
                    break;
                case MediaProcessor.CharacterRecognition:
                    settingKey = Constants.AppSettings.MediaProcessorCharacterRecognitionId;
                    processorId = AppSetting.GetValue(settingKey);
                    break;
                case MediaProcessor.ObjectDetection:
                    settingKey = Constants.AppSettings.MediaProcessorObjectDetectionId;
                    processorId = AppSetting.GetValue(settingKey);
                    break;
                case MediaProcessor.FaceDetection:
                    settingKey = Constants.AppSettings.MediaProcessorFaceDetectionId;
                    processorId = AppSetting.GetValue(settingKey);
                    break;
                case MediaProcessor.FaceRedaction:
                    settingKey = Constants.AppSettings.MediaProcessorFaceRedactionId;
                    processorId = AppSetting.GetValue(settingKey);
                    break;
                case MediaProcessor.MotionDetection:
                    settingKey = Constants.AppSettings.MediaProcessorMotionDetectionId;
                    processorId = AppSetting.GetValue(settingKey);
                    break;
                case MediaProcessor.MotionHyperlapse:
                    settingKey = Constants.AppSettings.MediaProcessorMotionHyperlapseId;
                    processorId = AppSetting.GetValue(settingKey);
                    break;
                case MediaProcessor.MotionStabilization:
                    settingKey = Constants.AppSettings.MediaProcessorMotionStabilizationId;
                    processorId = AppSetting.GetValue(settingKey);
                    break;
            }
            return processorId;
        }

        private INotificationEndPoint GetNotificationEndpoint()
        {
            string endpointName = Constants.Media.Job.NotificationEndpointName;
            INotificationEndPoint notificationEndpoint = GetEntityByName(EntityType.NotificationEndpoint, endpointName, true) as INotificationEndPoint;
            if (notificationEndpoint == null)
            {
                string queueName = Constants.Storage.QueueName.JobStatus;
                NotificationEndPointType endpointType = NotificationEndPointType.AzureQueue;
                notificationEndpoint = _media.NotificationEndPoints.Create(endpointName, endpointType, queueName);
            }
            return notificationEndpoint;
        }

        internal static string GetNotificationMessage(string accountName, IJob job)
        {
            string messageText = string.Concat("Azure Media Services Job ", job.State.ToString(), ".");
            messageText = string.Concat(messageText, " Account Name: ", accountName);
            messageText = string.Concat(messageText, ", Job Name: ", job.Name);
            messageText = string.Concat(messageText, ", Job ID: ", job.Id);
            return string.Concat(messageText, ", Job Running Duration: ", job.RunningDuration.ToString(Constants.FormatTime));
        }

        public static MediaJob CreateJob(MediaClient mediaClient, string jobName, int jobPriority, MediaJobTask[] jobTasks,
                                         MediaAssetInput[] inputAssets, ContentProtection contentProtection)
        {
            MediaJob mediaJob = new MediaJob();
            mediaJob.Name = jobName;
            mediaJob.Priority = jobPriority;

            List<MediaJobTask> taskList = new List<MediaJobTask>();
            foreach (MediaJobTask jobTask in jobTasks)
            {
                MediaJobTask[] tasks = null;
                switch (jobTask.MediaProcessor)
                {
                    case MediaProcessor.EncoderStandard:
                    case MediaProcessor.EncoderPremium:
                        tasks = GetEncoderTasks(mediaClient, jobTask, inputAssets, contentProtection);
                        break;
                    case MediaProcessor.IndexerV2:
                        tasks = GetIndexerTasks(mediaClient, jobTask, inputAssets, contentProtection);
                        break;
                }
                if (tasks != null)
                {
                    taskList.AddRange(tasks);
                }
            }
            mediaJob.Tasks = taskList.ToArray();
            if (string.IsNullOrEmpty(mediaJob.Name))
            {
                string taskName = mediaJob.Tasks[0].Name;
                string assetId = inputAssets[0].AssetId;
                IAsset asset = mediaClient.GetEntityById(EntityType.Asset, assetId) as IAsset;
                mediaJob.Name = string.Concat(taskName, " (", asset.Name, ")");
            }
            return mediaJob;
        }

        public IJob CreateJob(MediaJob mediaJob)
        {
            IJob job = _media.Jobs.Create(mediaJob.Name, mediaJob.Priority);
            foreach (MediaJobTask jobTask in mediaJob.Tasks)
            {
                string processorId = GetProcessorId(jobTask.MediaProcessor);
                IMediaProcessor processor = GetEntityById(EntityType.Processor, processorId) as IMediaProcessor;
                ITask currentTask = job.Tasks.AddNew(jobTask.Name, processor, jobTask.ProcessorConfig, jobTask.Options);
                if (jobTask.ParentIndex.HasValue)
                {
                    ITask parentTask = job.Tasks[jobTask.ParentIndex.Value];
                    currentTask.InputAssets.AddRange(parentTask.OutputAssets);
                }
                else
                {
                    IAsset[] assets = GetAssets(jobTask.InputAssetIds);
                    currentTask.InputAssets.AddRange(assets);
                }
                currentTask.OutputAssets.AddNew(jobTask.OutputAssetName, jobTask.OutputAssetEncryption);
            }

            INotificationEndPoint notificationEndpoint = GetNotificationEndpoint();
            NotificationJobState notificationState = NotificationJobState.FinalStatesOnly;
            job.JobNotificationSubscriptions.AddNew(notificationState, notificationEndpoint);

            job.Submit();
            return job;
        }

        public static void TrackJob(string authToken, IJob job, string storageAccount, ContentProtection contentProtection)
        {
            string attributeName = Constants.UserAttribute.MediaAccountName;
            string accountName = AuthToken.GetClaimValue(authToken, attributeName);

            attributeName = Constants.UserAttribute.MediaAccountKey;
            string accountKey = AuthToken.GetClaimValue(authToken, attributeName);

            attributeName = Constants.UserAttribute.MobileNumber;
            string mobileNumber = AuthToken.GetClaimValue(authToken, attributeName);

            contentProtection.PartitionKey = accountName;
            contentProtection.RowKey = job.Id;

            ContentPublish contentPublish = new ContentPublish();
            contentPublish.PartitionKey = contentProtection.PartitionKey;
            contentPublish.RowKey = contentProtection.RowKey;

            contentPublish.MediaAccountKey = accountKey;
            contentPublish.StorageAccountName = storageAccount;
            contentPublish.StorageAccountKey = Storage.GetAccountKey(authToken, storageAccount);
            contentPublish.MobileNumber = mobileNumber;

            EntityClient entityClient = new EntityClient();

            string tableName = Constants.Storage.TableNames.AssetPublish;
            entityClient.InsertEntity(tableName, contentPublish);

            if (contentProtection!= null)
            {
                tableName = Constants.Storage.TableNames.AssetProtection;
                entityClient.InsertEntity(tableName, contentProtection);
            }
        }
    }
}
