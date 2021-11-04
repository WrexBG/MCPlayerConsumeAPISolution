﻿using MCPlayerApiClient.DTOs;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace MCPlayerApiClient.ApiClient
{
    public class PlayerApiClient : IPlayerApiClient
    {
        private RestClient _restClient;
        public PlayerApiClient(string apiUri) => _restClient = new RestClient(new Uri(apiUri));

        public async Task<IEnumerable<string>> GetAllNamesAsync(string uuid)
        {
            RestClient client = new($"https://api.mojang.com/");

            RestRequest request = new("user/profiles/{uuid}/names", Method.GET);
            request.AddParameter("uuid", uuid, ParameterType.UrlSegment);
            request.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };

            IRestResponse queryResult = await client.ExecuteAsync(request);

            List<NameChanges> names = JsonSerializer.Deserialize<List<NameChanges>>(queryResult.Content);

            IEnumerable<string> GetListOfNames()
            {
                foreach (NameChanges player in names)
                {
                    yield return player.name;
                }
            }

            return GetListOfNames();
        }

        public async Task<Image> GetBodyImageFromUUIDAsync(string uuid)
        {
            return await GetImageAsync($"https://mc-heads.net/body/{uuid}");
        }

        public async Task<string> GetUUIDFromNameAsync(string name, int scale)
        {
            RestClient client = new($"https://api.mojang.com/");

            RestRequest request = new("users/profiles/minecraft/{name}?scale={scale}", Method.GET);
            request.AddParameter("name", name, ParameterType.UrlSegment);
            request.AddParameter("scale", scale, ParameterType.UrlSegment);

            request.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };

            IRestResponse queryResult = await client.ExecuteAsync(request);

            PlayerUUIDTuple player = JsonSerializer.Deserialize<PlayerUUIDTuple>(queryResult.Content);
            return player.id;
        }

        private async Task<Image> GetImageAsync(string url)
        {
            var taskCompletionSource = new TaskCompletionSource<Image>();
            Image webImage = null;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            await Task.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, null).ContinueWith(task =>
            {
                var webResponse = (HttpWebResponse)task.Result;
                Stream responseStream = webResponse.GetResponseStream();
                webImage = Image.FromStream(responseStream);
                taskCompletionSource.TrySetResult(webImage);
                webResponse.Close();
                responseStream.Close();
            });
            return taskCompletionSource.Task.Result;
        }

        public Task<Player> GetPlayerFromUUID(string uuid)
        {
            throw new NotImplementedException();
        }

        internal class PlayerUUIDTuple
        {
            public string name { get; set; }
            public string id { get; set; }
        }

        public class NameChanges
        {
            public string name { get; set; }
            public long changedToAt { get; set; }
        }
    }
}