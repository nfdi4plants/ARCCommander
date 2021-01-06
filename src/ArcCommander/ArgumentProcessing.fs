namespace ArcCommander

open System

open System.Reflection
open Microsoft.FSharp.Reflection

open System.Diagnostics

open IsaXLSX

open Argu


module ArgumentProcessing = 

    /// Carries the argument value to the ArcCommander API functions, use 'containsFlag' and 'getFieldValuByName' to access the value
    type Argument =
        | Field of string
        | Flag

    /// Argument with additional information
    type AnnotatedArgument = 
        {
            Arg         : Argument Option
            Tooltip     : string
            IsMandatory : bool
            IsFlag      : bool
        }

    
    let createAnnotatedArgument arg tt mand isFlag = 
        {
            Arg = arg
            Tooltip = tt 
            IsMandatory = mand
            IsFlag = isFlag
        }

    /// Returns true, if the argument flag of name k was given by the user
    let containsFlag k (arguments : Map<string,Argument>)=
        match Map.tryFind k arguments with
        | Some (Field _ )   -> failwithf "Argument %s is not a flag, but a field" k
        | Some (Flag)       -> true
        | None              -> false

    /// Returns the value given by the user for name k
    let tryGetFieldValueByName k (arguments : Map<string,Argument>) = 
        match Map.tryFind k arguments with
        | Some (Field v) -> Some v
        | Some Flag -> None
        | None -> None

    /// Returns the value given by the user for name k
    let getFieldValueByName k (arguments : Map<string,Argument>) = 
        match Map.find k arguments with
        | Field v -> v
        | Flag -> failwithf "Argument %s is not a field, but a flag" k

    /// For a given discriminated union value, returns the field name and the value
    let private splitUnion (x:'a) = 
        match FSharpValue.GetUnionFields(x, typeof<'a>) with
        | case, v -> case.Name,v

    /// For a given discriminated union value, returns the field name and the value string
    let private unionToString (x:'a) = 
        match splitUnion x with
        | (field,value) -> field, string value.[0]


    /// Returns given attribute from property info as optional 
    let private containsCustomAttribute<'a> (case : UnionCaseInfo) =   
        let attributeType = typeof<'a>
        match case.GetCustomAttributes (attributeType) with
        | [||] -> false
        | _ -> true
   
    /// Returns true, if a value in the array contains the Mandatory attribute, but is empty
    let private containsMissingMandatoryAttribute (arguments:(string*AnnotatedArgument) []) =
        arguments
        |> Seq.exists (fun (k,v) ->
            v.Arg.IsNone && v.IsMandatory
        )

    /// Adds all union cases of 'T which are missing to the list
    let groupArguments (args : 'T list when 'T :> IArgParserTemplate) =
        let m = 
            args 
            |> List.map splitUnion
            |> Map.ofList
        FSharpType.GetUnionCases(typeof<'T>)
        |> Array.map (fun unionCase ->
            let isMandatory = containsCustomAttribute<MandatoryAttribute>(unionCase) 
            let fields = unionCase.GetFields()
            match fields with 
            | [||] -> 
                let toolTip = (FSharpValue.MakeUnion (unionCase,[||]) :?> 'T).Usage
                let value,isFlag = if Map.containsKey unionCase.Name m then Some Flag,true else None,true             
                unionCase.Name,createAnnotatedArgument value toolTip isMandatory isFlag
            | [|c|] when c.PropertyType.Name = "String" -> 
                let toolTip = (FSharpValue.MakeUnion (unionCase,[|box ""|]) :?> 'T).Usage
                let value,isFlag = 
                    match Map.tryFind unionCase.Name m with
                    | Some value -> 
                        Field (string value.[0])
                        |> Some,
                        false
                    | None -> None,false                   
                unionCase.Name,createAnnotatedArgument value toolTip isMandatory isFlag
            | _ ->
                failwithf "Cannot parse argument %s because its parsing rules were not yet implemented" unionCase.Name
        )

    ///// Creates an isa item used in the investigation file
    //let isaItemOfArguments (item:#InvestigationFile.ISAItem) (parameters : Map<string,Argument>) = 
    //    parameters
    //    |> Map.iter (fun k v -> 
    //        match v with
    //        | Field s -> 
    //            InvestigationFile.setKeyValue (System.Collections.Generic.KeyValuePair(k,s)) item
    //            |> ignore
    //        | Flag ->
    //            ()
    //    )
    //    item

    /// Functions for asking the user to input values via an editor prompt
    module Prompt = 

        /// Create a MD5 hash from an input string
        let private MD5Hash (input : string) =
            use md5 = System.Security.Cryptography.MD5.Create()
            input
            |> System.Text.Encoding.ASCII.GetBytes
            |> md5.ComputeHash
            |> Seq.map (fun c -> c.ToString("X2"))
            |> Seq.reduce (+)
    
        /// Starts a program at the given path with the given arguments
        let private runProcess rootPath arg =           
            let p = 
                new ProcessStartInfo
                    (rootPath,arg) 
                |> Process.Start
            p.WaitForExit()

        /// Deletes the file
        let private delete (path:string) = 
            System.IO.File.Delete(path) 

        /// Writes a text string to a path
        let private write (path:string) (text:string) = 
            use w = new System.IO.StreamWriter(path)
            w.Write(text)
            w.Flush()
            w.Close()
        
        /// Writes a text string to a path, Creates the directory if it doens't yet exist
        let private writeForce (path:string) (text:string) = 
            delete path
            System.IO.FileInfo(path).Directory.Create()
            write path text

        /// Reads the content of a file
        let private read (path:string) = 
            use r = new System.IO.StreamReader(path)
            r.ReadToEnd()
    

        /// Serializes annotated argument in yaml format (key:value)
        ///
        /// For each value, a comment is created and put above the line using the given commentF function
        let private serializeAnnotatedArguments (arguments:(string*AnnotatedArgument) []) =
            let header = 
                """# Not all mandatory input arguments were given
# Please fill out at least all mandatory fields, commented out flags (arguments with no value) can be set by removing the # in front of them
# When finished save and close the editor"""
            arguments
            |> Array.map (fun (key,arg) -> 
                let comment = 
                    if arg.IsMandatory then
                        sprintf "Mandatory: %s" arg.Tooltip
                    elif arg.IsFlag then
                        sprintf "Remove # below to set flag: %s" arg.Tooltip
                    else sprintf "%s" arg.Tooltip
                let value = 
                    match arg.Arg with
                    | Some (Flag)           -> sprintf "%s" key
                    | Some (Field v)        -> sprintf "%s:%s" key v
                    | None when arg.IsFlag  -> sprintf "#%s" key
                    | None                  -> sprintf "%s:" key
                sprintf "#%s\n%s" comment value
            )
            |> Array.reduce (fun a b -> a + "\n" + b)
    
        /// Splits the string at the first occurence of the char 
        let private splitAtFirst (c:char) (s:string) =
            match s.Split c with
            | [|k|]     -> k.Trim(), Flag 
            | [|k ; v|] -> k.Trim(), v.Trim() |> Field 
            | a         -> a.[0].Trim(), Array.skip 1 a |> Array.reduce (fun a b ->sprintf "%s%c%s" a c b) |> fun v -> v.Trim() |> Field


        /// Deserializes yaml format (key:value) arguments
        let private deserializeArguments (s:string) =
            s.Split '\n'
            |> Array.choose (fun x -> 
                if x.StartsWith "#" then
                    None
                else 
                    splitAtFirst ':' x |> Some
            )

        /// Opens a textprompt containing the result of the serializeF to the user. Returns the deserialized user input
        let createQuery editorPath arcPath serializeF deserializeF inp =
            let yamlString = serializeF inp
            let hash = MD5Hash yamlString
            let filePath = sprintf "%s/.arc/%s.yml" arcPath hash
    
            writeForce filePath yamlString
            try
                runProcess editorPath filePath
                let p = read filePath |> deserializeF           
                delete filePath
                p

            with
            | err -> 
                delete filePath
                failwithf "could not parse query: %s" err.Message

        /// Opens a textprompt containing the result of the serialized input parameters. Returns the deserialized user input
        let createArgumentQuery editorPath arcPath (arguments:(string*AnnotatedArgument) []) = 
            arguments
            |> createQuery editorPath arcPath serializeAnnotatedArguments deserializeArguments 
            |> Map.ofArray

        /// If parameters are missing a mandatory field, opens a textprompt containing the result of the serialized input parameters. Returns the deserialized user input
        let createArgumentQueryIfNecessary editorPath arcPath (arguments:(string*AnnotatedArgument) []) = 
            if containsMissingMandatoryAttribute arguments then             
                let mandatoryArgs = arguments |> Array.choose (fun (key,arg) -> if arg.IsMandatory then Some key else None)
                let queryResults = createArgumentQuery editorPath arcPath arguments
                let stillMissingMandatoryArgs =  
                    mandatoryArgs
                    |> Array.map (fun k -> 
                        let field = tryGetFieldValueByName k queryResults
                        field = None || field = Some ""                    
                    )
                    |> Array.reduce ((||))
                stillMissingMandatoryArgs,queryResults
            else 
                false,
                arguments
                |> Array.choose (fun (k,v) -> 
                    match v.Arg with 
                    | Some arg -> Some (k,arg)
                    | None when v.IsFlag -> None
                    | None -> Some (k,Field ""))
                |> Map.ofArray

        /// Open a textprompt containing the serialized input item. Returns item updated with the deserialized user input
        let createIsaItemQuery editorPath arcPath 
            (writeF : 'A -> seq<DocumentFormat.OpenXml.Spreadsheet.Row>)
            (readF : System.Collections.Generic.IEnumerator<DocumentFormat.OpenXml.Spreadsheet.Row> -> 'A)
            (isaItem : 'A) = 

            let serializeF (inp : 'A) = 
                writeF inp
                |> Seq.map (fun r -> 
                    sprintf "%s:%s"
                        (FSharpSpreadsheetML.Row.tryGetValueAt 1u r |> Option.get |> fun s -> s.TrimStart())
                        (FSharpSpreadsheetML.Row.tryGetValueAt 2u r |> Option.get)
                )
                |> Seq.reduce (fun a b -> a + "\n" + b)
            let deserializeF (s:string) : 'A =
                s.Split '\n'
                |> Seq.map (fun x ->                 
                    match splitAtFirst ':' x with
                    | k, Field v ->
                        FSharpSpreadsheetML.Row.ofValues 1u [k;v]
                    | _ -> failwith "Error: file was corrupted in Edtior"
                )
                |> fun rs -> readF (rs.GetEnumerator()) 
            createQuery editorPath arcPath serializeF deserializeF isaItem

        

        /// Open a textprompt containing the serialized iniData. Returns the iniData updated with the deserialized user input
        let createIniDataQuery editorPath arcPath (iniData : IniParser.Model.IniData) =
            let serializeF inp = 
                IniData.flatten inp
                |> Seq.map (fun (n,v) -> n + "=" + v)
                |> Seq.reduce (fun a b -> a + "\n" + b)
            let deserializeF (s:string) =
                s.Split '\n'
                |> Array.map (fun x ->      
                    match splitAtFirst '=' x with
                    | k, Field v -> k,v
                    | _ -> failwith "Error: file was corrupted in Edtior"
                )              
                |> IniData.fromNameValuePairs
            createQuery editorPath arcPath serializeF deserializeF iniData 
