module Docs

open Giraffe.ViewEngine

let private head = 
    head [] [
        meta [_charset "utf-8"]
        meta [_name "viewport"; _content "width=device-width, initial-scale=1"]
        meta [_name "description"; _content "SwaggerUI"]
        title [] [str "SwaggerUI"]
        link [_rel "shortcut icon"; _type "image/png"; _href "https://raw.githubusercontent.com/nfdi4plants/Branding/master/icons/DataPLANT/favicons/favicon_bg_transparent.png" ]
        link [_rel "stylesheet"; _href "https://unpkg.com/swagger-ui-dist@4.5.0/swagger-ui.css"]
    ]

let private js_binder (yamlFileName:string) = 
    sprintf """
        const url = new URL('./%s', window.location.origin)
        window.onload = () => {
            window.ui = SwaggerUIBundle({
                url: url.href,
                dom_id: '#swagger-ui',
            });
        };
    """ yamlFileName

let private body (yamlFileName:string) =
    body [] [
        div [_id "swagger-ui"] []
        script [_src "https://unpkg.com/swagger-ui-dist@4.5.0/swagger-ui-bundle.js"; _crossorigin ""] []
        script [] [
            rawText (js_binder yamlFileName)
        ]
    ]

let baseViews (yamlFileName:string) =
    html [] [
        head
        body yamlFileName
    ]