using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TelegramBot.Models;
using System.Web;
using Google.Apis.Auth.OAuth2;
using System.Net.Http;
using Newtonsoft.Json;
using Google.Apis.YouTube.v3;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Util.Store;
using Google.Apis.Auth.OAuth2.Responses;

namespace TelegramBot
{
    public class GoogleAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public GoogleAuthMiddleware(RequestDelegate next)
        {
            this._next = next;
        }

        // 0. Get the code
        // 1. Request POST https://www.googleapis.com/oauth2/v4/token with params code={}&client_id={client_id}&client_secret={}&redirect_uri={redirect_uri}&grant_type=authorization_code
        // 2. Google responsds with JSON object that contais access token (short live period), refresh token (NOTE: refresh token returns only if access_type=offline in initial requst), 
        //      expires_in (the remaining lifetime of the access token in seconds), token_type (=Bearer)
        // 3. Access Google APIs with access_token (for example, GET https://www.googleapis.com/drive/v2/files?access_token=<access_token>)
        // 4. Refresh access_token if needed: Request POST https://www.googleapis.com/oauth2/v4/token with params refresh_token={refresh_token}&client_id={client_id}&client_secret={}&grant_type=refresh_token.
        // 5. Google respods with the JSON object with access_token, expires_in and token_type (see pt 2)
        // 6. Revoke token if needed by request to https://accounts.google.com/o/oauth2/revoke?token={token}

        public async Task InvokeAsync(HttpContext context) //не менять на синхронный, прога руинится!!
        {
            if (context.Request.Query.ContainsKey("code") && context.Request.Query.ContainsKey("state"))
            {
                var code = context.Request.Query["code"];
                var state = context.Request.Query["state"].ToString();
                var userId = state.Split('_')[0];
                var chatId = state.Split('_')[1];
                var user = Bot.GetUserById(userId);
                await user.SetCredentialAsync(chatId, code);
                long id;
                long.TryParse(chatId, out id);
                await PubSubHubbub.PuSH.AddAllChannelsAsync(id, user);

            }
            else if (context.Request.Query.ContainsKey("error") && context.Request.Query.ContainsKey("state"))
            {
                var error = context.Request.Query["error"];
                var state = context.Request.Query["state"];
                await Bot.SendDebugMessageAsync($"An error occured during Google Authentication Proccess. Message: {error.ToString()}");
            }

            context.Response.Redirect(AppSettings.BotUrl);
        }

    }
}

