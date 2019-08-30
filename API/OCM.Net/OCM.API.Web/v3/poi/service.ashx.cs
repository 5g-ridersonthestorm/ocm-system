﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCM.API.V3.POI
{
    /// <summary>
    /// POI API Endpoint Handler
    /// </summary>
    public class APIEndpoint : APICoreHTTPHandler
    {
        public APIEndpoint()
        {
            this.APIBehaviourVersion = 3;
            this.DefaultAction = "poi";
            this.IsQueryByPost = false;
        }
    }
}