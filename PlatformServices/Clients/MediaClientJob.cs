﻿using System.Collections.Generic;
using System.Collections.Specialized;

using Microsoft.WindowsAzure.MediaServices.Client;

namespace AzureSkyMedia.PlatformServices
{
    internal partial class MediaClient
    {
        private INotificationEndPoint GetNotificationEndpoint()
        {
            string endpointName = Constant.Media.JobNotification.EndpointName;
            INotificationEndPoint notificationEndpoint = GetEntityByName(MediaEntity.NotificationEndpoint, endpointName, true) as INotificationEndPoint;
            if (notificationEndpoint == null)
            {
                NotificationEndPointType endpointType = NotificationEndPointType.WebHook;
                string settingKey = Constant.AppSettingKey.MediaPublishContentUrl;
                string endpointAddress = AppSetting.GetValue(settingKey);
                notificationEndpoint = _media.NotificationEndPoints.Create(endpointName, endpointType, endpointAddress);
            }
            return notificationEndpoint;
        }

        private void SetProcessorUnits(IJob job, IJobTemplate jobTemplate, ReservedUnitType nodeType, bool newJob)
        {
            int unitCount = jobTemplate != null ? jobTemplate.TaskTemplates.Count : job.Tasks.Count;
            IEncodingReservedUnit[] processorUnits = GetEntities(MediaEntity.ProcessorUnit) as IEncodingReservedUnit[];
            IEncodingReservedUnit processorUnit = processorUnits[0];
            processorUnit.ReservedUnitType = nodeType;
            if (newJob)
            {
                processorUnit.CurrentReservedUnits += unitCount;
            }
            else if (processorUnit.CurrentReservedUnits >= unitCount)
            {
                processorUnit.CurrentReservedUnits -= unitCount;
            }
            processorUnit.Update();
        }

        internal static MediaJob GetJob(string authToken, MediaClient mediaClient, MediaJob mediaJob, MediaJobInput[] jobInputs, out ContentIndex indexerConfig)
        {
            indexerConfig = null;
            List<MediaJobTask> jobTasks = new List<MediaJobTask>();
            foreach (MediaJobTask jobTask in mediaJob.Tasks)
            {
                MediaJobTask[] tasks = null;
                switch (jobTask.MediaProcessor)
                {
                    case MediaProcessor.EncoderStandard:
                    case MediaProcessor.EncoderPremium:
                        tasks = GetEncoderTasks(mediaClient, jobTask, jobInputs);
                        break;
                    case MediaProcessor.VideoIndexer:
                        IndexerClient indexerClient = new IndexerClient(authToken);
                        if (indexerClient.IndexerEnabled)
                        {
                            if (HasEncoderTask(mediaJob.Tasks))
                            {
                                indexerConfig = jobTask.ContentIndex;
                            }
                            else
                            {
                                foreach (MediaJobInput jobInput in jobInputs)
                                {
                                    IAsset asset = mediaClient.GetEntityById(MediaEntity.Asset, jobInput.AssetId) as IAsset;
                                    string documentId = DocumentClient.GetDocumentId(asset, out bool videoIndexer);
                                    if (videoIndexer)
                                    {
                                        indexerClient.ResetIndex(documentId);
                                    }
                                    else
                                    {
                                        User authUser = new User(authToken);
                                        jobTask.ContentIndex.MediaAccountDomainName = authUser.MediaAccountDomainName;
                                        jobTask.ContentIndex.MediaAccountEndpointUrl = authUser.MediaAccountEndpointUrl;
                                        jobTask.ContentIndex.MediaAccountClientId = authUser.MediaAccountClientId;
                                        jobTask.ContentIndex.MediaAccountClientKey = authUser.MediaAccountClientKey;
                                        jobTask.ContentIndex.IndexerAccountKey = authUser.VideoIndexerKey;
                                        indexerClient.IndexVideo(mediaClient, asset, jobTask.ContentIndex);
                                    }
                                }
                            }
                        }
                        break;
                    case MediaProcessor.VideoAnnotation:
                        tasks = GetVideoAnnotationTasks(mediaClient, jobTask, jobInputs);
                        break;
                    case MediaProcessor.VideoSummarization:
                        tasks = GetVideoSummarizationTasks(mediaClient, jobTask, jobInputs);
                        break;
                    case MediaProcessor.CharacterRecognition:
                        tasks = GetCharacterRecognitionTasks(mediaClient, jobTask, jobInputs);
                        break;
                    case MediaProcessor.ContentModeration:
                        tasks = GetContentModerationTasks(mediaClient, jobTask, jobInputs);
                        break;
                    case MediaProcessor.SpeechAnalyzer:
                        tasks = GetSpeechAnalyzerTasks(mediaClient, jobTask, jobInputs);
                        break;
                    case MediaProcessor.FaceDetection:
                        tasks = GetFaceDetectionTasks(mediaClient, jobTask, jobInputs);
                        break;
                    case MediaProcessor.FaceRedaction:
                        tasks = GetFaceRedactionTasks(mediaClient, jobTask, jobInputs);
                        break;
                    case MediaProcessor.MotionDetection:
                        tasks = GetMotionDetectionTasks(mediaClient, jobTask, jobInputs);
                        break;
                    case MediaProcessor.MotionHyperlapse:
                        tasks = GetMotionHyperlapseTasks(mediaClient, jobTask, jobInputs);
                        break;
                }
                if (tasks != null)
                {
                    jobTasks.AddRange(tasks);
                }
            }
            mediaJob.Tasks = jobTasks.ToArray();
            if (string.IsNullOrEmpty(mediaJob.Name))
            {
                mediaJob.Name = jobInputs[0].AssetName;
            }
            return mediaJob;
        }

        internal IJob CreateJob(MediaJob mediaJob, MediaJobInput[] jobInputs, out IJobTemplate jobTemplate)
        {
            IJob job = null;
            jobTemplate = null;
            if (!string.IsNullOrEmpty(mediaJob.TemplateId))
            {
                List<IAsset> inputAssets = new List<IAsset>();
                foreach (MediaJobInput jobInput in jobInputs)
                {
                    IAsset asset = GetEntityById(MediaEntity.Asset, jobInput.AssetId) as IAsset;
                    if (asset != null)
                    {
                        inputAssets.Add(asset);
                    }
                }
                jobTemplate = GetEntityById(MediaEntity.JobTemplate, mediaJob.TemplateId) as IJobTemplate;
                job = _media.Jobs.Create(mediaJob.Name, jobTemplate, inputAssets, mediaJob.Priority);
            }
            else if (mediaJob.Tasks.Length > 0)
            {
                job = _media.Jobs.Create(mediaJob.Name, mediaJob.Priority);
                foreach (MediaJobTask jobTask in mediaJob.Tasks)
                {
                    string processorId = Processor.GetProcessorId(jobTask.MediaProcessor);
                    IMediaProcessor processor = GetEntityById(MediaEntity.Processor, processorId) as IMediaProcessor;
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
                    currentTask.OutputAssets.AddNew(jobTask.OutputAssetName, jobTask.OutputAssetEncryption, jobTask.OutputAssetFormat);
                }
                INotificationEndPoint notificationEndpoint = GetNotificationEndpoint();
                job.JobNotificationSubscriptions.AddNew(NotificationJobState.FinalStatesOnly, notificationEndpoint);
            }
            if (job != null)
            {
                if (mediaJob.SaveWorkflow)
                {
                    string templateName = mediaJob.Name;
                    jobTemplate = job.SaveAsTemplate(templateName);
                }
                else
                {
                    SetProcessorUnits(job, jobTemplate, mediaJob.NodeType, true);
                    job.Submit();
                }
            }
            return job;
        }

        public static NameValueCollection GetJobTemplates(string authToken)
        {
            NameValueCollection jobTemplates = new NameValueCollection();
            MediaClient mediaClient = new MediaClient(authToken);
            IJobTemplate[] templates = mediaClient.GetEntities(MediaEntity.JobTemplate) as IJobTemplate[];
            foreach (IJobTemplate template in templates)
            {
                jobTemplates.Add(template.Name, template.Id);
            }
            return jobTemplates;
        }
    }
}