namespace ArcCommander

open IdentityModel.OidcClient;
open System;
open System.Collections.Generic;
open System.Diagnostics;
open System.IO;
open System.Runtime.InteropServices;
open System.Text;
open System.Text.Json
open System.Text.Json.Serialization
open System.Threading.Tasks;
//open JWT.Builder
//open JWT.Algorithms

module Authentication =

    //let decodeResponse (response : string) = 
        
    //    JwtBuilder.Create()
    //    |> fun b -> b.WithAlgorithm(new HMACSHA256Algorithm()) // symmetric
    //    //|> fun b -> b.WithSecret(secret)
    //    //|> fun b -> b.MustVerifySignature()
    //    |> fun b -> b.Decode(response)

    /// Fields returned by the token service
    type IdentityToken =    
        {
            [<JsonPropertyName(@"exp")>]
            Id : int
            [<JsonPropertyName(@"git-access-token")>]
            GitAccessToken : string
            [<JsonPropertyName(@"git-host")>]
            GitHost : string
            [<JsonPropertyName(@"given_name")>]
            FirstName : string
            [<JsonPropertyName(@"family_name")>]
            LastName : string
            [<JsonPropertyName(@"preferred_username")>]
            UserName : string
            [<JsonPropertyName(@"email")>]
            Email : string
            [<JsonPropertyName(@"email_verified")>]
            EmailVerified : bool
            //RefreshToken : string
        }

        static member ofJson (jsonString : string) =
            ISADotNet.JsonExtensions.fromString<IdentityToken> jsonString

        //static member ofJwt (jwtResponse : string) =
        //    decodeResponse jwtResponse
        //    |> IdentityToken.ofJson
    
    /// Create a standard html site text with given string as body
    let fillHTML s = $"<html><head></head><body><h1>{s}</h1></body></html>"
        
    /// Open the the given url in the standard browser of the operating system
    let openBrowser (url : string) =
    
        try
            Process.Start(url)
        with
        | _ when RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ->
            let url = url.Replace("&", "^&")
            let psi = new ProcessStartInfo("cmd", $"/c start {url}")
            psi.CreateNoWindow <- true
            Process.Start(psi)
        | _ when RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ->
            Process.Start("xdg-open", url)
        | _ when RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ->         
            Process.Start("open", url);
        | e ->
            raise e
   
    /// Write the response string to the stream of the response context. This function is used to fill the the redirect page after user login.
    let sendResponseAsync (responseString : string) (responseContext : System.Net.HttpListenerResponse) =
        task {
            
            let buffer = Encoding.UTF8.GetBytes(responseString)

            responseContext.ContentLength64 <- int64 buffer.Length
    
            let responseOutput = responseContext.OutputStream;
            let! _ = responseOutput.WriteAsync(buffer, 0, buffer.Length)
            responseOutput.Flush();
        }

    /// Try obtaining the token information from the request
    let tryProcessRequestAsync (client : OidcClient) (state : AuthorizeState) (requestContext : System.Net.HttpListenerRequest) =
        task {
            try
                let! result = client.ProcessResponseAsync(requestContext.RawUrl, state);
                return Result.Ok result
            with
            | err -> 
                return FSharp.Core.Result.Error err
        }

    /// Get the options needed for an authorization request from the arc configuration
    let loadOptionsFromConfig authority (arcConfiguration : ArcConfiguration) =
        new OidcClientOptions(
            Authority =     authority,
            ClientId =      GeneralConfiguration.getAuthClientID arcConfiguration,
            Scope =         GeneralConfiguration.getAuthScope arcConfiguration,
            RedirectUri =   GeneralConfiguration.getAuthRedirectURI arcConfiguration
        )

    module Oidc =

        /// Get the token information from a token service specified in the options
        let signInAsync (log : NLog.Logger) (options : OidcClientOptions) =
            task {

                log.Info($"Start login at {options.Authority}")

                log.Trace($"Starting local listener for obtaining token service response at {options.RedirectUri}")

                let http = new System.Net.HttpListener()
                if options.RedirectUri.EndsWith "/" |> not then options.RedirectUri <- $"{options.RedirectUri}/"

                http.Prefixes.Add(options.RedirectUri)
                http.Start();

                log.Trace("Prepare client for login procedure")

                let client = new OidcClient(options)
                let! state = client.PrepareLoginAsync()
    
                log.Trace($"Open Browser at {state.StartUrl}")

                openBrowser(state.StartUrl) |> ignore
    
                log.Info("Waiting for user login")
                http.Start()
                let! context = http.GetContextAsync()
    
                log.Trace("Try processing request")

                let! result = tryProcessRequestAsync client state context.Request

                log.Trace("Try sending response to browser")

                match result with
                | Result.Ok r ->
                    let! _ = sendResponseAsync (fillHTML "Success") context.Response
                    return result
                | FSharp.Core.Result.Error err ->
                    let failureString = 
                        sprintf "Could not parse request: \n %s" err.Message
                        |> fillHTML
                    let! _ = sendResponseAsync failureString context.Response
                    return result
            }

        /// Try to get the token information from a token service specified in the arc configuration
        [<STAThread>]
        let tryLogin (log : NLog.Logger) authority (arcConfiguration : ArcConfiguration) =
            
            log.Info("Initiate login protocol")

            log.Trace("Load token service options from config")

            let options = loadOptionsFromConfig authority arcConfiguration            

            let t = signInAsync log options

            t.Wait()
            t.Result
            //|> Result.map (fun result -> IdentityToken.ofJwt result.IdentityToken)

    module OAuth2 =

        /// Try to get the token information from a token service specified in the arc configuration
        [<STAThread>]
        let tryLogin (log : NLog.Logger) authority (arcConfiguration : ArcConfiguration) =

            raise (NotImplementedException("Login via OAuth2 authorization protocol is not yet implemented"))