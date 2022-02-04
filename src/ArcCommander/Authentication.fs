namespace ArcCommander

open IdentityModel.OidcClient;
open Microsoft.Net.Http.Server;
open System;
open System.Collections.Generic;
open System.Diagnostics;
open System.IO;
open System.Runtime.InteropServices;
open System.Text;
open System.Text.Json
open System.Text.Json.Serialization
open System.Threading.Tasks;
open JWT.Builder
open JWT.Algorithms

module Authentication =

    let decodeResponse (response : string) = 
        
        JwtBuilder.Create()
        |> fun b -> b.WithAlgorithm(new HMACSHA256Algorithm()) // symmetric
        //|> fun b -> b.WithSecret(secret)
        //|> fun b -> b.MustVerifySignature()
        |> fun b -> b.Decode(response)

    type IdentityToken =    
        {
            [<JsonPropertyName(@"exp")>]
            Id : int
            [<JsonPropertyName(@"gitlab-token-attr")>]
            GitLabToken : string
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

        static member ofJwt (jwtResponse : string) =
            decodeResponse jwtResponse
            |> IdentityToken.ofJson
    

    let fillHTML s = $"<html><head></head><body><h1>{s}</h1></body></html>"
        
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
   
    let sendResponseAsync (responseString : string) (responseContext : Response) =
        task {
            
            let buffer = Encoding.UTF8.GetBytes(responseString)
    
            responseContext.ContentLength <- int64 buffer.Length
    
            let responseOutput = responseContext.Body;
            let! _ = responseOutput.WriteAsync(buffer, 0, buffer.Length)
            responseOutput.Flush();
        }

    let tryProcessRequestAsync (client : OidcClient) (state : AuthorizeState) (requestContext : Request) =
        task {
            try
                let! result = client.ProcessResponseAsync(requestContext.RawUrl, state);
                return Result.Ok result
            with
            | err -> 
                return FSharp.Core.Result.Error err
        }

    let signInAsync (options : OidcClientOptions) =
        task {
            // create a redirect URI using an available port on the loopback address.

            // create an HttpListener to listen for requests on that redirect URI.
            let settings = new WebListenerSettings()
            settings.UrlPrefixes.Add(options.RedirectUri)
            let http = new WebListener(settings)
    
            http.Start();
    
            //let serilog = 
            //    LoggerConfiguration()
            //        .MinimumLevel.Verbose()
            //        .Enrich.FromLogContext()
            //        .WriteTo.LiterateConsole(outputTemplate = "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message}{NewLine}{Exception}{NewLine}")
            //        .CreateLogger()
    
            //options.LoggerFactory.AddSerilog(serilog) |> ignore
    
            let client = new OidcClient(options)
            let! state = client.PrepareLoginAsync()
    
            openBrowser(state.StartUrl) |> ignore
    
            //let! _ = Task.Delay(1000)
            let! context = http.AcceptAsync()
    
            let! result = tryProcessRequestAsync client state context.Request

            match result with
            | Result.Ok result ->
                let! _ = sendResponseAsync (fillHTML "Success") context.Response
                return Some result
            | FSharp.Core.Result.Error err ->
                let failureString = 
                    sprintf "Could not parse request: \n %s" err.Message
                    |> fillHTML
                let! _ = sendResponseAsync failureString context.Response
                return None
        }