﻿using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCM.API.Common
{
    public class ReferenceDataManager : ManagerBase
    {
        public Country GetCountryByName(string country)
        {
            if (country == null) return null;
            country = country.ToLower();
            var selectedCountry = dataModel.Countries.FirstOrDefault(c => c.Title.ToLower() == country.ToLower() || c.Title.ToLower().Replace(" ", "") == country.Replace(" ", ""));
            return OCM.API.Common.Model.Extensions.Country.FromDataModel(selectedCountry);
        }

        public Country GetCountryByISO(string countryISO)
        {
            if (countryISO == null) return null;

            countryISO = countryISO.Trim().ToUpper();

            var selectedCountry = dataModel.Countries.FirstOrDefault(c => c.Isocode == countryISO);
            return OCM.API.Common.Model.Extensions.Country.FromDataModel(selectedCountry);
        }

        public Country GetCountry(int countryId)
        {
            return OCM.API.Common.Model.Extensions.Country.FromDataModel(dataModel.Countries.Find(countryId));
        }

        public List<Country> GetCountries(bool withPOIOnly)
        {
            if (withPOIOnly)
            {
                var poiCollection = new OCM.Core.Data.CacheProviderMongoDB().GetPOICollection();
                //determine all countries with live POI
                var allPOICountries = (from cp in poiCollection.FindAll()
                                       where cp.SubmissionStatus.IsLive == true
                                       select cp.AddressInfo.CountryID).Distinct();

                return OCM.API.Common.Model.Extensions.Country.FromDataModel(dataModel.Countries.Where(c => allPOICountries.Contains(c.Id)).OrderBy(c => c.Title));
            }
            else
            {
                return OCM.API.Common.Model.Extensions.Country.FromDataModel(dataModel.Countries.OrderBy(c => c.Title));
            }
        }

        public List<OperatorInfo> GetOperators(int? countryId)
        {
            if (countryId != null)
            {
                return OCM.API.Common.Model.Extensions.OperatorInfo.FromDataModel(
                    dataModel.Operators.Where(c => c.ChargePoints.Any(cp => cp.AddressInfo.CountryId == countryId && cp.SubmissionStatusType.IsLive == true)).OrderBy(c => c.Title)
                    );
            }
            else
            {
                return OCM.API.Common.Model.Extensions.OperatorInfo.FromDataModel(dataModel.Operators.OrderBy(c => c.Title));
            }
        }

        public List<DataProvider> GetDataProviders()
        {
            var dataProviders = new List<Model.DataProvider>();
            foreach (var provider in dataModel.DataProviders.ToList())
            {
                dataProviders.Add(Model.Extensions.DataProvider.FromDataModel(provider));
            }
            return dataProviders;
        }

        public CoreReferenceData GetCoreReferenceData(bool enableCaching = true)
        {
            CoreReferenceData data = null;

            if (enableCaching)
            {
                data = OCM.Core.Data.CacheManager.GetCoreReferenceData();

                if (data != null) return data;
            }

            //can't get cached data, get fresh from database
            data = new CoreReferenceData();

            //list of Levels (ChargerTypes)
            data.ChargerTypes = new List<Model.ChargerType>();
            foreach (var cg in dataModel.ChargerTypes)
            {
                data.ChargerTypes.Add(Model.Extensions.ChargerType.FromDataModel(cg));
            }

            //list of connection types
            data.ConnectionTypes = new List<Model.ConnectionType>();
            foreach (var ct in dataModel.ConnectionTypes.OrderBy(d=>d.Title))
            {
                data.ConnectionTypes.Add(Model.Extensions.ConnectionType.FromDataModel(ct));
            }

            //list of power source types (AC/DC etc)
            data.CurrentTypes = new List<Model.CurrentType>();
            foreach (var ct in dataModel.CurrentTypes)
            {
                data.CurrentTypes.Add(Model.Extensions.CurrentType.FromDataModel(ct));
            }

            //list of countries
            data.Countries = new List<Model.Country>();
            foreach (var country in dataModel.Countries)
            {
                data.Countries.Add(Model.Extensions.Country.FromDataModel(country));
            }

            //list of Data Providers
            data.DataProviders = new List<Model.DataProvider>();
            foreach (var provider in dataModel.DataProviders.ToList())
            {
                data.DataProviders.Add(Model.Extensions.DataProvider.FromDataModel(provider));
            }

            //list of Operators
            data.Operators = new List<Model.OperatorInfo>();
            foreach (var source in dataModel.Operators.OrderBy(o => o.Title))
            {
                data.Operators.Add(Model.Extensions.OperatorInfo.FromDataModel(source));
            }

            //list of Status Types
            data.StatusTypes = new List<Model.StatusType>();
            foreach (var status in dataModel.StatusTypes)
            {
                data.StatusTypes.Add(Model.Extensions.StatusType.FromDataModel(status));
            }

            //list of Usage Types (public etc)
            data.UsageTypes = new List<Model.UsageType>();
            foreach (var usage in dataModel.UsageTypes.OrderBy(u => u.Title))
            {
                data.UsageTypes.Add(Model.Extensions.UsageType.FromDataModel(usage));
            }

            //list of user comment types
            data.UserCommentTypes = new List<Model.UserCommentType>();
            foreach (var commentType in dataModel.UserCommentTypes)
            {
                data.UserCommentTypes.Add(Model.Extensions.UserCommentType.FromDataModel(commentType));
            }

            //list of user comment types
            data.CheckinStatusTypes = new List<Model.CheckinStatusType>();
            foreach (var checkinType in dataModel.CheckinStatusTypes)
            {
                data.CheckinStatusTypes.Add(Model.Extensions.CheckinStatusType.FromDataModel(checkinType));
            }

            data.SubmissionStatusTypes = new List<Model.SubmissionStatusType>();
            foreach (var s in dataModel.SubmissionStatusTypes)
            {
                data.SubmissionStatusTypes.Add(Model.Extensions.SubmissionStatusType.FromDataModel(s));
            }

            data.MetadataGroups = new List<Model.MetadataGroup>();
            foreach (var g in dataModel.MetadataGroups.ToList())
            {
                data.MetadataGroups.Add(Model.Extensions.MetadataGroup.FromDataModel(g));
            }

            data.DataTypes = new List<Model.DataType>();
            foreach (var d in dataModel.DataTypes)
            {
                data.DataTypes.Add(Model.Extensions.DataType.FromDataModel(d));
            }
            data.ChargePoint = new ChargePoint()
            {
                AddressInfo = new Model.AddressInfo(),
#pragma warning disable 612 //suppress obsolete warning
                Chargers = new List<Model.ChargerInfo> { new Model.ChargerInfo() },
#pragma warning restore 612
                Connections = new List<Model.ConnectionInfo> { new Model.ConnectionInfo() },
                DateCreated = DateTime.UtcNow,
                DateLastConfirmed = DateTime.UtcNow,
                DateLastStatusUpdate = DateTime.UtcNow,
                GeneralComments = "",
                DatePlanned = null,
                ID = -1,
                NumberOfPoints = 1,
                StatusType = new Model.StatusType(),
                OperatorInfo = new Model.OperatorInfo(),
                DataProvider = new Model.DataProvider(),
                UsageType = new Model.UsageType(),
                UUID = Guid.NewGuid().ToString(),
                DataQualityLevel = 1
            };

            data.UserComment = new Model.UserComment { ChargePointID = 0, Comment = "", CommentType = data.UserCommentTypes[0], DateCreated = DateTime.UtcNow, ID = 0, CheckinStatusType = data.CheckinStatusTypes[0] };
            return data;
        }
    }
}