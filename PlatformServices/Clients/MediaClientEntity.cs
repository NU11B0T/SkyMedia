﻿using System.Collections.Generic;

using Microsoft.Rest.Azure;
using Microsoft.Rest.Azure.OData;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AzureSkyMedia.PlatformServices
{
    internal partial class MediaClient
    {
        public IDictionary<string, string> GetCorrelationData(JObject jsonData, bool addAccount)
        {
            Dictionary<string, string> correlationData = new Dictionary<string, string>();
            if (addAccount)
            {
                string userAccount = JsonConvert.SerializeObject(this.UserAccount);
                string mediaAccount = JsonConvert.SerializeObject(this.MediaAccount);
                correlationData.Add("userAccount", userAccount);
                correlationData.Add("mediaAccount", mediaAccount);
            }
            if (jsonData != null)
            {
                foreach (KeyValuePair<string, JToken> property in jsonData)
                {
                    string propertyData = property.Value.ToString();
                    correlationData.Add(property.Key, propertyData);
                }
            }
            return correlationData;
        }

        public IDictionary<string, string> GetCorrelationData(string jsonData, bool addAccount)
        {
            IDictionary<string, string> correlationData = null;
            if (!string.IsNullOrEmpty(jsonData))
            {
                correlationData = GetCorrelationData(JObject.Parse(jsonData), addAccount);
            }
            return correlationData;
        }

        public int GetEntityCount<T>(MediaEntity entityType) where T : Resource
        {
            T[] entities = GetAllEntities<T>(entityType);
            return entities.Length;
        }

        public int GetEntityCount<T1, T2>(MediaEntity entityType, MediaEntity parentType) where T1 : Resource where T2 : Resource
        {
            T1[] entities = GetAllEntities<T1, T2>(entityType, parentType);
            return entities.Length;
        }

        public T1[] GetAllEntities<T1, T2>(MediaEntity entityType, MediaEntity parentType) where T1 : Resource where T2 : Resource
        {
            List<T1> allEntities = new List<T1>();
            IPage<T2> parentEntities = GetEntities<T2>(parentType);
            foreach (T2 parentEntity in parentEntities)
            {
                T1[] entities = GetAllEntities<T1>(entityType, null, parentEntity.Name);
                allEntities.AddRange(entities);
            }
            return allEntities.ToArray();
        }

        public T[] GetAllEntities<T>(MediaEntity entityType, string queryFilter = null, string parentName = null) where T : Resource
        {
            List<T> allEntities = new List<T>();
            IPage<T> entities = GetEntities<T>(entityType, queryFilter, parentName);
            while (entities != null)
            {
                allEntities.AddRange(entities);
                entities = NextEntities(entityType, entities);
            }
            return allEntities.ToArray();
        }

        public IPage<T> GetEntities<T>(MediaEntity entityType, string queryFilter = null, string parentName = null) where T : Resource
        {
            IPage<T> entities = null;
            switch (entityType)
            {
                case MediaEntity.Asset:
                    ODataQuery<Asset> assetQuery = new ODataQuery<Asset>()
                    {
                        Filter = queryFilter
                    };
                    entities = (IPage<T>)_media.Assets.List(MediaAccount.ResourceGroupName, MediaAccount.Name, assetQuery);
                    break;
                case MediaEntity.Transform:
                    ODataQuery<Transform> transformQuery = new ODataQuery<Transform>()
                    {
                        Filter = queryFilter
                    };
                    entities = (IPage<T>)_media.Transforms.List(MediaAccount.ResourceGroupName, MediaAccount.Name, transformQuery);
                    break;
                case MediaEntity.TransformJob:
                    ODataQuery<Job> transformJobQuery = new ODataQuery<Job>()
                    {
                        Filter = queryFilter
                    };
                    entities = (IPage<T>)_media.Jobs.List(MediaAccount.ResourceGroupName, MediaAccount.Name, parentName, transformJobQuery);
                    break;
                case MediaEntity.ContentKeyPolicy:
                    ODataQuery<ContentKeyPolicy> contentKeyPolicyQuery = new ODataQuery<ContentKeyPolicy>()
                    {
                        Filter = queryFilter
                    };
                    entities = (IPage<T>)_media.ContentKeyPolicies.List(MediaAccount.ResourceGroupName, MediaAccount.Name, contentKeyPolicyQuery);
                    break;
                case MediaEntity.StreamingPolicy:
                    ODataQuery<StreamingPolicy> streamingPolicyQuery = new ODataQuery<StreamingPolicy>()
                    {
                        Filter = queryFilter
                    };
                    entities = (IPage<T>)_media.StreamingPolicies.List(MediaAccount.ResourceGroupName, MediaAccount.Name, streamingPolicyQuery);
                    break;
                case MediaEntity.StreamingEndpoint:
                    entities = (IPage<T>)_media.StreamingEndpoints.List(MediaAccount.ResourceGroupName, MediaAccount.Name);
                    break;
                case MediaEntity.StreamingLocator:
                    ODataQuery<StreamingLocator> streamingLocatorQuery = new ODataQuery<StreamingLocator>()
                    {
                        Filter = queryFilter
                    };
                    entities = (IPage<T>)_media.StreamingLocators.List(MediaAccount.ResourceGroupName, MediaAccount.Name, streamingLocatorQuery);
                    break;
                case MediaEntity.StreamingFilterAccount:
                    entities = (IPage<T>)_media.AccountFilters.List(MediaAccount.ResourceGroupName, MediaAccount.Name);
                    break;
                case MediaEntity.StreamingFilterAsset:
                    entities = (IPage<T>)_media.AssetFilters.List(MediaAccount.ResourceGroupName, MediaAccount.Name, parentName);
                    break;
                case MediaEntity.LiveEvent:
                    entities = (IPage<T>)_media.LiveEvents.List(MediaAccount.ResourceGroupName, MediaAccount.Name);
                    break;
                case MediaEntity.LiveEventOutput:
                    entities = (IPage<T>)_media.LiveOutputs.List(MediaAccount.ResourceGroupName, MediaAccount.Name, parentName);
                    break;
            }
            return entities;
        }

        public IPage<T> NextEntities<T>(MediaEntity entityType, IPage<T> currentPage)
        {
            IPage<T> entities = null;
            if (!string.IsNullOrEmpty(currentPage.NextPageLink))
            {
                switch (entityType)
                {
                    case MediaEntity.Asset:
                        entities = (IPage<T>)_media.Assets.ListNext(currentPage.NextPageLink);
                        break;
                    case MediaEntity.Transform:
                        entities = (IPage<T>)_media.Transforms.ListNext(currentPage.NextPageLink);
                        break;
                    case MediaEntity.TransformJob:
                        entities = (IPage<T>)_media.Jobs.ListNext(currentPage.NextPageLink);
                        break;
                    case MediaEntity.ContentKeyPolicy:
                        entities = (IPage<T>)_media.ContentKeyPolicies.ListNext(currentPage.NextPageLink);
                        break;
                    case MediaEntity.StreamingPolicy:
                        entities = (IPage<T>)_media.StreamingPolicies.ListNext(currentPage.NextPageLink);
                        break;
                    case MediaEntity.StreamingEndpoint:
                        entities = (IPage<T>)_media.StreamingEndpoints.ListNext(currentPage.NextPageLink);
                        break;
                    case MediaEntity.StreamingLocator:
                        entities = (IPage<T>)_media.StreamingLocators.ListNext(currentPage.NextPageLink);
                        break;
                    case MediaEntity.StreamingFilterAccount:
                        entities = (IPage<T>)_media.AccountFilters.ListNext(currentPage.NextPageLink);
                        break;
                    case MediaEntity.StreamingFilterAsset:
                        entities = (IPage<T>)_media.AssetFilters.ListNext(currentPage.NextPageLink);
                        break;
                    case MediaEntity.LiveEvent:
                        entities = (IPage<T>)_media.LiveEvents.ListNext(currentPage.NextPageLink);
                        break;
                    case MediaEntity.LiveEventOutput:
                        entities = (IPage<T>)_media.LiveOutputs.ListNext(currentPage.NextPageLink);
                        break;
                }
            }
            return entities;
        }

        public T GetEntity<T>(MediaEntity entityType, string entityName, string parentName = null) where T : Resource
        {
            T entity = default(T);
            switch (entityType)
            {
                case MediaEntity.Asset:
                    entity = _media.Assets.Get(MediaAccount.ResourceGroupName, MediaAccount.Name, entityName) as T;
                    break;
                case MediaEntity.Transform:
                    entity = _media.Transforms.Get(MediaAccount.ResourceGroupName, MediaAccount.Name, entityName) as T;
                    break;
                case MediaEntity.TransformJob:
                    entity = _media.Jobs.Get(MediaAccount.ResourceGroupName, MediaAccount.Name, parentName, entityName) as T;
                    break;
                case MediaEntity.ContentKeyPolicy:
                    entity = _media.ContentKeyPolicies.Get(MediaAccount.ResourceGroupName, MediaAccount.Name, entityName) as T;
                    break;
                case MediaEntity.StreamingPolicy:
                    entity = _media.StreamingPolicies.Get(MediaAccount.ResourceGroupName, MediaAccount.Name, entityName) as T;
                    break;
                case MediaEntity.StreamingEndpoint:
                    entity = _media.StreamingEndpoints.Get(MediaAccount.ResourceGroupName, MediaAccount.Name, entityName) as T;
                    break;
                case MediaEntity.StreamingLocator:
                    entity = _media.StreamingLocators.Get(MediaAccount.ResourceGroupName, MediaAccount.Name, entityName) as T;
                    break;
                case MediaEntity.StreamingFilterAccount:
                    entity = _media.AccountFilters.Get(MediaAccount.ResourceGroupName, MediaAccount.Name, entityName) as T;
                    break;
                case MediaEntity.StreamingFilterAsset:
                    entity = _media.AssetFilters.Get(MediaAccount.ResourceGroupName, MediaAccount.Name, parentName, entityName) as T;
                    break;
                case MediaEntity.LiveEvent:
                    entity = _media.LiveEvents.Get(MediaAccount.ResourceGroupName, MediaAccount.Name, entityName) as T;
                    break;
                case MediaEntity.LiveEventOutput:
                    entity = _media.LiveOutputs.Get(MediaAccount.ResourceGroupName, MediaAccount.Name, parentName, entityName) as T;
                    break;
            }
            return entity;
        }

        public void DeleteEntity(MediaEntity entityType, string entityName, string parentName = null)
        {
            switch (entityType)
            {
                case MediaEntity.Asset:
                    _media.Assets.Delete(MediaAccount.ResourceGroupName, MediaAccount.Name, entityName);
                    break;
                case MediaEntity.Transform:
                    Job[] jobs = GetAllEntities<Job>(MediaEntity.TransformJob, null, entityName);
                    foreach (Job job in jobs)
                    {
                        DeleteEntity(MediaEntity.TransformJob, job.Name, entityName);
                    }
                    _media.Transforms.Delete(MediaAccount.ResourceGroupName, MediaAccount.Name, entityName);
                    break;
                case MediaEntity.TransformJob:
                    _media.Jobs.Delete(MediaAccount.ResourceGroupName, MediaAccount.Name, parentName, entityName);
                    break;
                case MediaEntity.ContentKeyPolicy:
                    _media.ContentKeyPolicies.Delete(MediaAccount.ResourceGroupName, MediaAccount.Name, entityName);
                    break;
                case MediaEntity.StreamingPolicy:
                    _media.StreamingPolicies.Delete(MediaAccount.ResourceGroupName, MediaAccount.Name, entityName);
                    break;
                case MediaEntity.StreamingEndpoint:
                    _media.StreamingEndpoints.Delete(MediaAccount.ResourceGroupName, MediaAccount.Name, entityName);
                    break;
                case MediaEntity.StreamingLocator:
                    _media.StreamingLocators.Delete(MediaAccount.ResourceGroupName, MediaAccount.Name, entityName);
                    break;
                case MediaEntity.StreamingFilterAccount:
                    _media.AccountFilters.Delete(MediaAccount.ResourceGroupName, MediaAccount.Name, entityName);
                    break;
                case MediaEntity.StreamingFilterAsset:
                    _media.AssetFilters.Delete(MediaAccount.ResourceGroupName, MediaAccount.Name, parentName, entityName);
                    break;
                case MediaEntity.LiveEvent:
                    LiveEvent liveEvent = GetEntity<LiveEvent>(MediaEntity.LiveEvent, entityName);
                    if (liveEvent != null)
                    {
                        LiveOutput[] liveOutputs = GetAllEntities<LiveOutput>(MediaEntity.LiveEventOutput, null, entityName);
                        foreach (LiveOutput liveOutput in liveOutputs)
                        {
                            _media.LiveOutputs.Delete(MediaAccount.ResourceGroupName, MediaAccount.Name, entityName, liveOutput.Name);
                        }
                        _media.LiveEvents.Delete(MediaAccount.ResourceGroupName, MediaAccount.Name, entityName);
                    }
                    break;
                case MediaEntity.LiveEventOutput:
                    _media.LiveOutputs.Delete(MediaAccount.ResourceGroupName, MediaAccount.Name, parentName, entityName);
                    break;
            }
        }
    }
}