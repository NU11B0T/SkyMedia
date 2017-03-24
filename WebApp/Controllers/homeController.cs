﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Specialized;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

using AzureSkyMedia.PlatformServices;

namespace AzureSkyMedia.WebApp.Controllers
{
    public class homeController : Controller
    {
        private MediaTextTrack[] MapTextTracks(string textTracks)
        {
            List<MediaTextTrack> tracks = new List<MediaTextTrack>();
            if (!string.IsNullOrEmpty(textTracks))
            {
                string[] tracksInfo = textTracks.Split(Constant.TextDelimiter.Connection);
                foreach (string trackInfo in tracksInfo)
                {
                    MediaTextTrack track = new MediaTextTrack();
                    string[] textTrack = trackInfo.Split(Constant.TextDelimiter.Application);
                    track.Type = textTrack[0];
                    track.SourceUrl = textTrack[1];
                    track.LanguageCode = Language.GetLanguageCode(track.SourceUrl);
                    track.Label = Language.GetLanguageLabel(track.LanguageCode);
                    tracks.Add(track);
                }
            }
            return tracks.ToArray();
        }

        private MediaProtection[] MapProtectionTypes(string streamProtectionTypes)
        {
            List<MediaProtection> protectionTypes = new List<MediaProtection>();
            if (!string.IsNullOrEmpty(streamProtectionTypes))
            {
                string[] contentProtectionTypes = streamProtectionTypes.Split(Constant.TextDelimiter.Application);
                foreach (string contentProtectionType in contentProtectionTypes)
                {
                    MediaProtection protectionType = (MediaProtection)Enum.Parse(typeof(MediaProtection), contentProtectionType);
                    protectionTypes.Add(protectionType);
                }
            }
            return protectionTypes.ToArray();
        }

        private void AddBaseStream(List<MediaStream> mediaStreams, string settingStreamName, string settingSourceUrl,
                                   string settingTextTrack, string settingProtectionTypes)
        {
            string streamName = AppSetting.GetValue(settingStreamName);
            string sourceUrl = AppSetting.GetValue(settingSourceUrl);
            string textTracks = AppSetting.GetValue(settingTextTrack);
            string protectionTypes = AppSetting.GetValue(settingProtectionTypes);
            if (!string.IsNullOrEmpty(streamName))
            {
                MediaStream mediaStream = new MediaStream();
                mediaStream.Name = streamName;
                mediaStream.SourceUrl = sourceUrl;
                mediaStream.TextTracks = MapTextTracks(textTracks);
                mediaStream.ProtectionTypes = MapProtectionTypes(protectionTypes);
                mediaStream.AnalyticsMetadata = new MediaMetadata[] { };
                mediaStreams.Add(mediaStream);
            }
        }

        private MediaStream[] GetMediaStreams()
        {
            List<MediaStream> mediaStreams = new List<MediaStream>();

            string settingKey1 = Constant.AppSettingKey.MediaStream1Name;
            string settingKey2 = Constant.AppSettingKey.MediaStream1SourceUrl;
            string settingKey3 = Constant.AppSettingKey.MediaStream1TextTracks;
            string settingKey4 = Constant.AppSettingKey.MediaStream1ProtectionTypes;
            AddBaseStream(mediaStreams, settingKey1, settingKey2, settingKey3, settingKey4);

            settingKey1 = Constant.AppSettingKey.MediaStream2Name;
            settingKey2 = Constant.AppSettingKey.MediaStream2SourceUrl;
            settingKey3 = Constant.AppSettingKey.MediaStream2TextTracks;
            settingKey4 = Constant.AppSettingKey.MediaStream2ProtectionTypes;
            AddBaseStream(mediaStreams, settingKey1, settingKey2, settingKey3, settingKey4);

            settingKey1 = Constant.AppSettingKey.MediaStream3Name;
            settingKey2 = Constant.AppSettingKey.MediaStream3SourceUrl;
            settingKey3 = Constant.AppSettingKey.MediaStream3TextTracks;
            settingKey4 = Constant.AppSettingKey.MediaStream3ProtectionTypes;
            AddBaseStream(mediaStreams, settingKey1, settingKey2, settingKey3, settingKey4);

            return mediaStreams.ToArray();
        }

        private IActionResult GetLiveView(string channelName, string queryString)
        {
            string settingKey = Constant.AppSettingKey.AzureMedia;
            string[] accountCredentials = AppSetting.GetValue(settingKey, true);
            if (accountCredentials.Length > 0)
            {
                string accountName = accountCredentials[0];
                DateTime? liveEventStart = Stream.GetEventStart(accountName, channelName);
                if (liveEventStart.HasValue)
                {
                    bool livePreview = this.Request.Host.Value.Contains("preview") || queryString.Contains("preview");
                    ViewData["livePreview"] = livePreview;
                    ViewData["liveEventStart"] = liveEventStart.Value.ToString();
                    ViewData["liveSourceUrl"] = Stream.GetSourceUrl(channelName, livePreview);
                }
            }
            return View("live");
        }

        public static string GetAuthToken(HttpRequest request, HttpResponse response)
        {
            string authToken = null;
            string cookieKey = Constant.HttpCookie.UserAuthToken;
            if (request.HasFormContentType)
            {
                authToken = request.Form[Constant.HttpForm.IdToken];
                if (!string.IsNullOrEmpty(authToken))
                {
                    response.Cookies.Append(cookieKey, authToken);
                }
            }
            if (string.IsNullOrEmpty(authToken))
            {
                authToken = request.Cookies[cookieKey];
            }
            return authToken;
        }

        public static SelectListItem[] GetStorageAccounts(string authToken)
        {
            List<SelectListItem> storageAccounts = new List<SelectListItem>();
            NameValueCollection accounts = Storage.GetAccounts(authToken);
            foreach (string accountKey in accounts.Keys)
            {
                SelectListItem storageAccount = new SelectListItem();
                storageAccount.Text = accountKey;
                storageAccount.Value = accounts[accountKey];
                storageAccounts.Add(storageAccount);
            }
            return storageAccounts.ToArray();
        }

        public static SelectListItem[] GetJobTemplates(string authToken)
        {
            List<SelectListItem> jobTemplates = new List<SelectListItem>();
            NameValueCollection templates = MediaClient.GetJobTemplates(authToken);
            foreach (string templateKey in templates.Keys)
            {
                SelectListItem jobTemplate = new SelectListItem();
                jobTemplate.Text = templateKey;
                jobTemplate.Value = templates[templateKey];
                jobTemplates.Add(jobTemplate);
            }
            return jobTemplates.ToArray();
        }

        public static SelectListItem[] GetMediaProcessors(string authToken)
        {
            List<SelectListItem> mediaProcessors = new List<SelectListItem>();

            SelectListItem mediaProcessor = new SelectListItem();
            mediaProcessor.Text = string.Empty;
            mediaProcessor.Value = MediaProcessor.None.ToString();
            mediaProcessors.Add(mediaProcessor);

            NameValueCollection processors = Account.GetMediaProcessors(authToken, false) as NameValueCollection;
            foreach (string processor in processors)
            {
                mediaProcessor = new SelectListItem();
                mediaProcessor.Text = processor;
                mediaProcessor.Value = processors[processor];
                mediaProcessors.Add(mediaProcessor);
            }

            return mediaProcessors.ToArray();
        }

        public JsonResult command(string commandId, int parameterId, string parameterName, bool parameterFlag)
        {
            string authToken = GetAuthToken(this.Request, this.Response);
            MediaClient mediaClient = new MediaClient(authToken);
            switch (commandId)
            {
                case "channelCreate":
                    mediaClient.CreateChannel(parameterName);
                    break;
                case "channelSignal":
                    mediaClient.SignalChannel(parameterName, parameterId);
                    break;
                case "accountClear":
                    Account.ClearAccount(mediaClient, parameterFlag);
                    break;
            }
            string[][] entityCounts = Account.GetEntityCounts(mediaClient);
            return Json(entityCounts);
        }

        public IActionResult index()
        {
            string authToken = GetAuthToken(this.Request, this.Response);

            if (this.Request.HasFormContentType)
            {
                string requestError = this.Request.Form["error_description"];
                if (!string.IsNullOrEmpty(requestError) && requestError.Contains("AADB2C90118"))
                {
                    return RedirectToAction("passwordreset", "account");
                }

                try
                {
                    CacheClient cacheClient = new CacheClient(authToken);
                    cacheClient.Initialize(authToken);
                }
                catch (Exception ex)
                {
                    if (Debugger.IsAttached)
                    {
                        throw ex;
                    }
                }
            }

            string queryString = this.Request.QueryString.Value.ToLower();
            if (this.Request.Host.Value.Contains("account."))
            {
                return RedirectToAction("signin", "account");
            }
            else if (this.Request.Host.Value.Contains("live.") || queryString.Contains("live"))
            {
                string channelName = this.Request.Query["channel"];
                return GetLiveView(channelName, queryString);
            }

            MediaClient mediaClient = null;
            if (!string.IsNullOrEmpty(authToken))
            {
                try
                {
                    mediaClient = new MediaClient(authToken);
                }
                catch
                {
                    return RedirectToAction("profileedit", "account");
                }
            }

            MediaStream[] mediaStreams;
            string accountMessage = string.Empty;
            if (mediaClient == null)
            {
                mediaStreams = GetMediaStreams();
            }
            else if (!Account.IsStreamingEnabled(mediaClient))
            {
                mediaStreams = new MediaStream[] { };
                accountMessage = Constant.Message.StreamingEndpointNotRunning;
            }
            else
            {
                mediaStreams = Stream.GetMediaStreams(mediaClient);
            }
            ViewData["mediaStreams"] = mediaStreams;
            ViewData["accountMessage"] = accountMessage;

            ViewData["streamNumber"] = 1;
            ViewData["autoPlay"] = "false";
            if (queryString.Contains("stream"))
            {
                ViewData["streamNumber"] = this.Request.Query["stream"];
                ViewData["autoPlay"] = "true";
            }

            ViewData["languageCode"] = this.Request.Query["language"];
            return View();
        }
    }
}
