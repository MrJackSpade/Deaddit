﻿using Deaddit.Core.Interfaces;
using Reddit.Api;
using Reddit.Api.Interfaces;
using Reddit.Api.Json.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Core.Reddit
{
    public class DeadditClient : RedditClient
    {
        private readonly IDisplayMessages _displayMessages;

        public DeadditClient(IDisplayMessages displayMessages, IRedditCredentials redditCredentials, IJsonClient jsonClient, HttpClient httpClient) : base(redditCredentials, jsonClient, httpClient)
        {
            _displayMessages = displayMessages;
        }

        public override async Task<bool> DisplayException(Exception ex)
        {
            return await _displayMessages.DisplayException(ex);
        }
    }
}