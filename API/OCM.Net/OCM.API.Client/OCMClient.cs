﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OCM.API.Client
{
    public class APICredentials
    {
        public string Identifier { get; set; }
        public string SessionToken { get; set; }
    }

    public class SearchFilters
    {
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Distance { get; set; }
        public DistanceUnit DistanceUnit { get; set; }

        public bool IncludeUserComments { get; set; }
        public int MaxResults { get; set; }
        public bool EnableCaching { get; set; }

        public int[] DataProviderIDs { get; set; }
        public int[] SubmissionStatusTypeIDs { get; set; }
        public int[] CountryIDs { get; set; }

        public SearchFilters()
        {
            DistanceUnit = Common.Model.DistanceUnit.Miles;
            MaxResults = 100;
            EnableCaching = true;
        }
    }

    public class OCMClient
    {
        public bool IsSandboxMode { get; set; }
        public string ServiceBaseURL { get; set; }

        public OCMClient(bool sandBoxMode = false)
        {
            this.IsSandboxMode = sandBoxMode;

            if (!IsSandboxMode)
            {
                ServiceBaseURL = "https://api.openchargemap.io/v3/";
            }
            else
            {
                ServiceBaseURL = "http://sandbox.api.openchargemap.io/v2/";
            }
        }

#if DEBUG

        private async Task<bool> RunAPITest(string serviceBaseURL, int testIterations)
        {
            this.ServiceBaseURL = serviceBaseURL;

            int lat = 52;
            int lng = 1;

            SearchFilters filters = new SearchFilters { Distance = 100, MaxResults = 1000, Latitude = lat, Longitude = lng, EnableCaching = false, IncludeUserComments = false };
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            for (int i = 1; i < testIterations; i++)
            {
                System.Diagnostics.Debug.WriteLine(i);
                //TODO; pass same sample lat/lng list to tests
                var r = new Random(DateTime.Now.Second);
                filters.Latitude = r.Next(50, 55);
                filters.Longitude = r.Next(-1, 1);

                var list = await this.GetLocations(filters);
                System.Diagnostics.Debug.WriteLine(i + ": POIs: " + list.Count);
            }

            stopwatch.Stop();

            System.Diagnostics.Debug.WriteLine("total ms " + this.ServiceBaseURL + " : " + stopwatch.Elapsed.TotalMilliseconds + " for " + testIterations + " iterations. avg " + (stopwatch.Elapsed.TotalMilliseconds / testIterations) + "ms per request");
            return true;
        }

        public void APITestTiming()
        {
            OCMClient client = new OCMClient();

            Task<bool> test1 = client.RunAPITest("http://localhost:3000/v2", 10000);
            test1.Wait();
            // Task<bool> test2 = client.RunAPITest("http://localhost:8080/v2",1000);
            //test2.Wait();
        }

#endif

        /// <summary>
        /// Get core reference data such as lookup lists
        /// </summary>
        /// <returns>  </returns>
        public async Task<CoreReferenceData> GetCoreReferenceData()
        {
            //get core reference data
            string url = ServiceBaseURL + "/referencedata/?output=json&enablecaching=false";
            try
            {
                string data = await FetchDataStringFromURLAsync(url);
                JObject o = JObject.Parse(data);
                JsonSerializer serializer = new JsonSerializer();
                CoreReferenceData c = (CoreReferenceData)serializer.Deserialize(new JTokenReader(o), typeof(CoreReferenceData));
                return c;
            }
            catch (Exception)
            {
                //failed!
                return null;
            }
        }

        /// <summary>
        /// get list of matching POIs via API
        /// </summary>
        /// <param name="cp">  </param>
        /// <returns>  </returns>
        public async Task<List<ChargePoint>> GetLocations(SearchFilters filters)
        {
            string url = ServiceBaseURL + "/poi/?output=json&verbose=false";

            if (filters.Latitude != null && filters.Longitude != null)
            {
                url += "&latitude=" + filters.Latitude + "&longitude=" + filters.Longitude;
            }

            if (filters.Distance != null)
            {
                url += "&distance=" + filters.Distance + "&distanceunit=" + filters.DistanceUnit.ToString();
            }

            if (filters.EnableCaching == false)
            {
                url += "&enablecaching=false";
            }

            if (filters.IncludeUserComments == true)
            {
                url += "&includecomments=true";
            }

            if (filters.SubmissionStatusTypeIDs != null && filters.SubmissionStatusTypeIDs.Any())
            {
                url += "&submissionstatustypeid=";
                foreach (var id in filters.SubmissionStatusTypeIDs)
                {
                    url += id + ",";
                }
            }

            if (filters.DataProviderIDs != null && filters.DataProviderIDs.Any())
            {
                url += "&dataproviderid=";
                foreach (var id in filters.DataProviderIDs)
                {
                    url += id + ",";
                }
            }

            if (filters.CountryIDs != null && filters.CountryIDs.Any())
            {
                url += "&countryid=";
                foreach (var id in filters.CountryIDs)
                {
                    url += id + ",";
                }
            }

            url += "&maxresults=" + filters.MaxResults;

            try
            {
                System.Diagnostics.Debug.WriteLine("Client: Fetching data from " + url);
                string data = await FetchDataStringFromURLAsync(url);
                System.Diagnostics.Debug.WriteLine("Client: completed fetch");
                JObject o = JObject.Parse("{\"root\": " + data + "}");
                JsonSerializer serializer = new JsonSerializer();
                List<ChargePoint> c = (List<ChargePoint>)serializer.Deserialize(new JTokenReader(o["root"]), typeof(List<ChargePoint>));
                return c;
            }
            catch (Exception)
            {
                //failed!
                return null;
            }
        }

#if !PORTABLE

        public List<ChargePoint> FindSimilar(ChargePoint cp, int MinimumPercentageSimilarity)
        {
            List<ChargePoint> results = new List<ChargePoint>();
            try
            {
                string url = ServiceBaseURL + "?output=json&action=getsimilarchargepoints";

                string json = JsonConvert.SerializeObject(cp);
                string resultJSON = PostDataToURL(url, json);
                JObject o = JObject.Parse("{\"root\": " + resultJSON + "}");
                JsonSerializer serializer = new JsonSerializer();
                List<ChargePoint> c = (List<ChargePoint>)serializer.Deserialize(new JTokenReader(o["root"]), typeof(List<ChargePoint>));
                return c;
            }
            catch (Exception)
            {
                //failed!
                return null;
            }
        }

        public bool UpdateItem(ChargePoint cp, APICredentials credentials)
        {
            //TODO: implement batch update based on item list?
            try
            {
                string url = ServiceBaseURL + "?action=cp_submission&format=json";
                url += "&Identifier=" + credentials.Identifier;
                url += "&SessionToken=" + credentials.SessionToken;

                string json = JsonConvert.SerializeObject(cp);
                string result = PostDataToURL(url, json);
                return true;
            }
            catch (Exception)
            {
                //update failed
                System.Diagnostics.Debug.WriteLine("Update Item Failed: {" + cp.ID + ": " + cp.AddressInfo.Title + "}");
                return false;
            }
        }

#endif

        public APICredentials GetCredentials(string APIKey)
        {
            //TODO: implement in API, when API Key provided, return current credentials
            return new APICredentials() { };
        }

        public async Task<string> FetchDataStringFromURLAsync(string url)
        {
            HttpClient client = new HttpClient();
            //TODO: error handling
            HttpResponseMessage response = await client.GetAsync(url);

            string value = await response.Content.ReadAsStringAsync();
            return value;
        }

#if !PORTABLE

        public string PostDataToURL(string url, string data)
        {
            //http://msdn.microsoft.com/en-us/library/debx8sh9.aspx

            // Create a request using a URL that can receive a post.
            WebRequest request = WebRequest.Create(url);

            // Set the Method property of the request to POST.
            request.Method = "POST";

            // convert data to a byte array.
            byte[] byteArray = Encoding.UTF8.GetBytes(data);

            // Set the ContentType property of the WebRequest.
            request.ContentType = "application/x-www-form-urlencoded";

            // Set the ContentLength property of the WebRequest.
            request.ContentLength = byteArray.Length;

            // Get the request stream.
            using (Stream dataStream = request.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            // Get the response.
            using (WebResponse response = request.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        string statusDescription = ((HttpWebResponse)response).StatusDescription;
                        //TODO: return/handle error codes
                        //Get the stream containing content returned by the server.
                        string responseFromServer = reader.ReadToEnd();
                        return responseFromServer;
                    }
                }
            }
        }

#endif
    }
}