namespace ArcCommander

open System

open System.Reflection
open Microsoft.FSharp.Reflection

open System.Diagnostics
open YamlDotNet.Serialization;

open ISA.DataModel
open Argu.ArguAttributes

module ArgumentQuery = 

    

    let splitUnion (x:'a) = 
        match FSharpValue.GetUnionFields(x, typeof<'a>) with
        | case, v -> case.Name,v

    let unionToString (x:'a) = 
        match splitUnion x with
        | (field,value) -> field, string value.[0]


    /// Returns given attribute from property info as optional 
    let containsCustomAttribute<'a> (case : UnionCaseInfo) =   
        let attributeType = typeof<'a>
        match case.GetCustomAttributes (attributeType) with
        | [||] -> false
        | _ -> true
   
    
    let missingMandatoryAttribute (parameters : 'T []) =
        parameters

        |> Seq.exists (fun p ->
            let unionCaseInfo,s = FSharpValue.GetUnionFields(p,typeof<'T>)
            containsCustomAttribute<MandatoryAttribute> unionCaseInfo
            &&
            s = [|box ""|]
        )


    let groupArguments (args : 'T list) =
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
 
    let createParameterQuery editorPath arcPath (parameters : 'T []) = 
        let m =
            parameters
            |> Array.map unionToString
            |> Map.ofArray

        let serializer = SerializerBuilder().Build();
        let deserializer = DeserializerBuilder().Build();
         
        let yamlString = serializer.Serialize(m);
        let hash = MD5Hash yamlString
        let filePath = sprintf "%s/.arc/%s.txt" arcPath hash
    
        write filePath yamlString
        try
        runProcess editorPath filePath
        read filePath
        |> deserializer.Deserialize<Map<string,string>>
        |> Map.toArray

        with
        | err -> 
            failwithf "could not read query: %s" err.Message

    //let askForFillout (editorPath) (arcPath) (item:'T) =
    //    let serializer = SerializerBuilder().Build();
    //    let deserializer = DeserializerBuilder().Build();
         
    //    let yamlString = serializer.Serialize(item);
    //    let hash = MD5Hash yamlString
    //    let filePath = sprintf "%s/.arc/%s.txt" arcPath hash
    
    //    write filePath yamlString
    //    try
    //    runProcess editorPath filePath
    //    read filePath
    //    |> deserializer.Deserialize<'T>

    //    with
    //    | err -> 
    //        failwithf "could not read query: %s" err.Message
    
    //let askForFilloutIfNeeded (editorPath) (arcPath) (item:'T) =
    //    printfn "initial input: %O" item
    //    if containsAllRequiredValues item then
    //        printfn "is complete"
    //        item
    //    else 
    //        let inp = askForFillout editorPath arcPath item
    //        printfn "after querying input: %O" item
    //        inp