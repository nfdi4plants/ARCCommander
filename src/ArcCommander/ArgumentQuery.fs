namespace ArcCommander

open System

open System.Reflection
open Microsoft.FSharp.Reflection

open System.Diagnostics
//open YamlDotNet.Serialization;

open ISA.DataModel

open Argu
open Argu.ArguAttributes

module ArgumentQuery = 

    let private splitUnion (x:'a) = 
        match FSharpValue.GetUnionFields(x, typeof<'a>) with
        | case, v -> case.Name,v

    let private unionToString (x:'a) = 
        match splitUnion x with
        | (field,value) -> field, string value.[0]


    /// Returns given attribute from property info as optional 
    let private containsCustomAttribute<'a> (case : UnionCaseInfo) =   
        let attributeType = typeof<'a>
        match case.GetCustomAttributes (attributeType) with
        | [||] -> false
        | _ -> true
   
    
    let private missingMandatoryAttribute (parameters : 'T []) =
        parameters

        |> Seq.exists (fun p ->
            let unionCaseInfo,s = FSharpValue.GetUnionFields(p,typeof<'T>)
            containsCustomAttribute<MandatoryAttribute> unionCaseInfo
            &&
            s = [|box ""|]
        )


    let groupArguments (args : 'T list) =
        if typeof<'T>.Name = "EmptyArgs" then 
            [||]
        else
            let m = 
                args 
                |> List.map splitUnion
                |> Map.ofList
            FSharpType.GetUnionCases(typeof<'T>)
            |> Array.map (fun unionCase ->      
                let v = 
                    match Map.tryFind unionCase.Name m with
                    | Some value -> value
                    | None -> [|box ""|]
                FSharpValue.MakeUnion (unionCase,v) :?> 'T
            )


    let private MD5Hash (input : string) =
        use md5 = System.Security.Cryptography.MD5.Create()
        input
        |> System.Text.Encoding.ASCII.GetBytes
        |> md5.ComputeHash
        |> Seq.map (fun c -> c.ToString("X2"))
        |> Seq.reduce (+)
    
    let private runProcess rootPath arg =           
        let p = 
            new ProcessStartInfo
                (rootPath,arg) 
            |> Process.Start
        p.WaitForExit()

    let private writeForce (path:string) (text:string) = 
        System.IO.FileInfo(path).Directory.Create()
        use w = new System.IO.StreamWriter(path)
        w.Write(text)
        w.Flush()
        w.Close()

    let private write (path:string) (text:string) = 
        use w = new System.IO.StreamWriter(path)
        w.Write(text)
        w.Flush()
        w.Close()
    
    let private read (path:string) = 
        use r = new System.IO.StreamReader(path)
        r.ReadToEnd()
    
    let private delete (path:string) = 
        System.IO.File.Delete(path)
   
    let private serializeUnion (commentF : 'T -> string) (vs:'T []) =
        vs
        |> Array.map (fun v -> 
            let comment = commentF v
            let key,value = unionToString v
            sprintf "#%s\n%s:%s" comment key value
        )
        |> Array.reduce (fun a b -> a + "\n" + b)

    let private serializeMap (vs:Map<string,string>) =
        vs
        |> Seq.map (fun kv -> 
            sprintf "%s:%s" kv.Key kv.Value
        )
        |> Seq.reduce (fun a b -> a + "\n" + b)
    
    let private splitAtFirst (c:char) (s:string) =
        s.Split c
        |> fun a -> 
            a.[0].Trim(),
            if a.Length > 2 then
                Array.skip 1 a |> Array.reduce (fun a b -> a + ":" + b) |> fun v -> v.Trim()
            else
                a.[1]

    let private deserializeMap (s:string) =
        s.Split '\n'
        |> Array.choose (fun x -> 
            if x.StartsWith "#" then
                None
            else 
                splitAtFirst ':' x                
                |> Some
        )
        |> Map.ofArray

    let writeGlobalParams arcPath (parameters : Map<string,string>) = 

        let filePath = sprintf "%s/.arc/globalParams.yml" arcPath 
        
        serializeMap parameters
        |> writeForce filePath

    let tryReadGlobalParams arcPath = 
        try 
            let filePath = sprintf "%s/.arc/globalParams.yml" arcPath 
            read filePath
            |> deserializeMap
            |> Some
        with
        | err ->
            printfn "Could not obtain global params: \n %s" err.Message
            None

    let createQuery editorPath arcPath serializeF deserializeF inp =
        let yamlString = serializeF inp
        let hash = MD5Hash yamlString
        let filePath = sprintf "%s/.arc/%s.yml" arcPath hash
    
        writeForce filePath yamlString
        try
        runProcess editorPath filePath
        read filePath
        |> deserializeF

        with
        | err -> 
            failwithf "could not parse query: %s" err.Message

    let createParameterQuery editorPath arcPath (parameters : 'T []) = 
         
        let commentF (v : 'T when 'T :> IArgParserTemplate) =
            if FSharpValue.GetUnionFields(v,typeof<'T>) |> fst |> containsCustomAttribute<MandatoryAttribute> then
                sprintf "Mandatory: %s" v.Usage
            else
                v.Usage

        parameters
        |> createQuery editorPath arcPath (serializeUnion commentF) deserializeMap 


    let createParameterQueryIfNecessary editorPath arcPath (parameters : 'T []) = 
        if missingMandatoryAttribute parameters then
            createParameterQuery editorPath arcPath parameters
        else 
            parameters
            |> Array.map unionToString
            |> Map.ofArray


    let createItemQuery editorPath arcPath (item : #InvestigationFile.ISAItem) = 
        let serializeF inp = 
            InvestigationFile.getKeyValues inp
            |> Array.map (fun (k,v) -> sprintf "%s:%s" k v)
            |> Array.reduce (fun a b -> a + "\n" + b)
        let deserializeF (s:string) =
            s.Split '\n'
            |> Array.iter (fun x ->                 
                x.Split ':'
                |> fun a -> System.Collections.Generic.KeyValuePair(a.[0],a.[1])
                |> fun kv -> InvestigationFile.setKeyValue kv item
                |> ignore
            )

            item
        createQuery editorPath arcPath serializeF deserializeF item

    //let itemOfParameters item (parameters : 'T []) = 
    //    parameters
    //    |> Array.iter (fun p ->
    //        unionToString p 
    //        |> fun (k,v) -> System.Collections.Generic.KeyValuePair(k,v)
    //        |> fun kv -> InvestigationFile.setKeyValue kv item
    //        |> ignore
    //    )
    //    item

    let itemOfParameters item (parameters : Map<string,string>) = 
        parameters
        |> Seq.iter (fun kv -> 
            InvestigationFile.setKeyValue kv item
            |> ignore
        )
        item
