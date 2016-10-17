﻿using System;
using System.IO;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.MediaServices.Client;

using Newtonsoft.Json.Linq;

using SkyMedia.ServiceBroker;
using SkyMedia.WebApp.Models;

namespace SkyMedia.WebApp.Controllers
{
    public class workflowController : Controller
    {
        private MediaAssetInput MapInputAsset(IAsset asset)
        {
            MediaAssetInput inputAsset = new MediaAssetInput();
            inputAsset.AssetId = asset.Id;
            inputAsset.AssetName = asset.Name;
            inputAsset.PrimaryFile = MediaClient.GetPrimaryFile(asset);
            return inputAsset;
        }

        private MediaAssetInput[] MapInputAssets(MediaClient mediaClient, MediaAssetInput[] assets)
        {
            List<MediaAssetInput> inputAssets = new List<MediaAssetInput>();
            foreach (MediaAssetInput asset in assets)
            {
                IAsset mediaAsset = mediaClient.GetEntityById(EntityType.Asset, asset.AssetId) as IAsset;
                MediaAssetInput inputAsset = new MediaAssetInput();
                inputAsset.AssetId = asset.AssetId;
                inputAsset.AssetName = mediaAsset.Name;
                inputAsset.PrimaryFile = MediaClient.GetPrimaryFile(mediaAsset);
                inputAsset.MarkIn = asset.MarkIn;
                inputAsset.MarkOut = asset.MarkOut;
                inputAssets.Add(inputAsset);
            }
            return inputAssets.ToArray();
        }

        private MediaAssetInput[] CreateInputAssets(string authToken, MediaClient mediaClient, string storageAccount, bool storageEncryption,
                                                    string inputAssetName, bool multipleFileAsset, bool publishInputAsset, string[] fileNames,
                                                    ContentProtection contentProtection)
        {
            List<MediaAssetInput> inputAssets = new List<MediaAssetInput>();
            if (multipleFileAsset)
            {
                IAsset asset = mediaClient.CreateAsset(authToken, inputAssetName, storageAccount, storageEncryption, fileNames);
                if (publishInputAsset)
                {
                    MediaClient.PublishAsset(mediaClient, asset, contentProtection);
                }
                MediaAssetInput inputAsset = MapInputAsset(asset);
                inputAssets.Add(inputAsset);
            }
            else
            {
                for (int i = 0; i < fileNames.Length; i++)
                {
                    string fileName = fileNames[i];
                    string assetName = Path.GetFileNameWithoutExtension(fileName);
                    IAsset asset = mediaClient.CreateAsset(authToken, assetName, storageAccount, storageEncryption, new string[] { fileName });
                    if (publishInputAsset)
                    {
                        MediaClient.PublishAsset(mediaClient, asset, contentProtection);
                    }
                    MediaAssetInput inputAsset = MapInputAsset(asset);
                    inputAssets.Add(inputAsset);
                }
            }
            return inputAssets.ToArray();
        }

        private void SetInputClips(MediaClient mediaClient, MediaAssetInput[] inputAssets)
        {
            for (int i = 0; i < inputAssets.Length; i++)
            {
                IAsset asset = mediaClient.GetEntityById(EntityType.Asset, inputAssets[i].AssetId) as IAsset;
                inputAssets[i].PrimaryFile = MediaClient.GetPrimaryFile(asset);
                if (!string.IsNullOrEmpty(inputAssets[i].MarkIn) && !string.IsNullOrEmpty(inputAssets[i].MarkOut))
                {
                    int markIn = Convert.ToInt32(Math.Floor(Convert.ToDecimal(inputAssets[i].MarkIn)));
                    int markOut = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(inputAssets[i].MarkOut)));
                    int clipDuration = markOut - markIn;
                    TimeSpan markInTime = new TimeSpan(0, 0, markIn);
                    TimeSpan clipDurationTime = new TimeSpan(0, 0, clipDuration);
                    inputAssets[i].MarkIn = markInTime.ToString(Constants.FormatTime);
                    inputAssets[i].MarkOut = clipDurationTime.ToString(Constants.FormatTime);
                }
            }
        }

        private IJob SubmitJob(string authToken, MediaClient mediaClient, string storageAccount, MediaAssetInput[] inputAssets,
                               string jobName, int jobPriority, MediaJobTask[] jobTasks, ContentProtection contentProtection)
        {
            IJob job = null;
            if (jobTasks != null && jobTasks.Length > 0)
            {
                MediaJob mediaJob = MediaClient.CreateJob(mediaClient, jobName, jobPriority, jobTasks, inputAssets, contentProtection);
                job = mediaClient.CreateJob(mediaJob);
                MediaClient.TrackJob(authToken, job, storageAccount, contentProtection);
            }
            return job;
        }

        public JsonResult start(string[] fileNames, string storageAccount, bool storageEncryption, string inputAssetName,
                                bool multipleFileAsset, bool publishInputAsset, MediaAssetInput[] inputAssets, string jobName,
                                int jobPriority, MediaJobTask[] jobTasks, ContentProtection contentProtection)
        {
            string authToken = AuthToken.GetValue(this.Request, this.Response);
            MediaClient mediaClient = new MediaClient(authToken);
            if (fileNames.Length > 0)
            {
                inputAssets = CreateInputAssets(authToken, mediaClient, storageAccount, storageEncryption, inputAssetName, multipleFileAsset, publishInputAsset, fileNames, contentProtection);
            }
            else
            {
                inputAssets = MapInputAssets(mediaClient, inputAssets);
                SetInputClips(mediaClient, inputAssets);
                DatabaseClient databaseClient = new DatabaseClient();
                foreach (MediaJobTask jobTask in jobTasks)
                {
                    if (!string.IsNullOrEmpty(jobTask.ProcessorDocument))
                    {
                        JObject processorConfig = databaseClient.GetDocument(jobTask.ProcessorDocument);
                        jobTask.ProcessorConfig = processorConfig.ToString();
                    }
                }
            }
            IJob job = SubmitJob(authToken, mediaClient, storageAccount, inputAssets, jobName, jobPriority, jobTasks, contentProtection);
            return (job != null) ? Json(job) : Json(inputAssets);
        }

        public IActionResult index()
        {
            string authToken = AuthToken.GetValue(this.Request, this.Response);
            uploadController.SetViewData(authToken, this.ViewData);
            return View();
        }
    }
}
