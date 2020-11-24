namespace ArcCommander

open System

open System.Reflection
open Microsoft.FSharp.Reflection

open System.Diagnostics

open ISA.DataModel

open Argu

module ArgumentProcessing = 

    type Argument =
        | Field of string
        | Flag of bool

    type AnnotatedArgument = 
        {
            Arg : Argument
            Tooltip : string
            IsMandatory : bool
        }

    let createAnnotatedArgument arg tt mand = 
        {
            Arg = arg
            Tooltip = tt 
            IsMandatory = mand
        }

    let containsFlag k (arguments : Map<string,Argument>)=
        match Map.find k arguments with
        | Flag b -> b
        | Field _ -> failwithf "Argument %s is not a flag, but a field" k

    let getFieldValueByName k (arguments : Map<string,Argument>) = 
        match Map.find k arguments with
        | Field v -> v
        | Flag _ -> failwithf "Argument %s is not a flag, but a field" k

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
    let private isMissingMandatoryAttribute (arguments:(string*AnnotatedArgument) []) =
        arguments
        |> Seq.exists (fun (k,v) ->
            match v.Arg with
            | Field s -> s = "" && v.IsMandatory
            | _ -> false
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
                let value = Flag (Map.containsKey unionCase.Name m)              
                unionCase.Name,createAnnotatedArgument value toolTip isMandatory
            | [|c|] when c.PropertyType.Name = "String" -> 
                let toolTip = (FSharpValue.MakeUnion (unionCase,[|box ""|]) :?> 'T).Usage
                let value = 
                    match Map.tryFind unionCase.Name m with
                    | Some value -> string value.[0]
                    | None -> ""
                    |> Field
                unionCase.Name,createAnnotatedArgument value toolTip isMandatory
            | _ ->
                failwithf "Cannot parse argument %s because its parsing rules were not yet implemented" unionCase.Name
        )

    /// Creates an isa item used in the investigation file
    let isaItemOfArguments (item:#InvestigationFile.ISAItem) (parameters : Map<string,Argument>) = 
        parameters
        |> Map.iter (fun k v -> 
            match v with
            | Field s -> 
                InvestigationFile.setKeyValue (System.Collections.Generic.KeyValuePair(k,s)) item
                |> ignore
            | Flag b ->
                ()
        )
        item

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
            arguments
            |> Array.map (fun (key,arg) -> 
                let comment = 
                    if arg.IsMandatory then
                        sprintf "Mandatory: %s" arg.Tooltip
                    else sprintf "%s" arg.Tooltip
                let value = 
                    match arg.Arg with
                    | Flag _ -> "false"
                    | Field v -> v
                sprintf "#%s\n%s:%s" comment key value
            )
            |> Array.reduce (fun a b -> a + "\n" + b)
    
        /// Splits the string at the first occurence of the char 
        let private splitAtFirst (c:char) (s:string) =
            s.Split c
            |> fun a -> 
                a.[0].Trim(),
                if a.Length > 2 then
                    Array.skip 1 a |> Array.reduce (fun a b -> a + ":" + b) |> fun v -> v.Trim()
                else
                    a.[1]

        /// Deserializes yaml format (key:value) arguments
        let private deserializeArguments (s:string) =
            s.Split '\n'
            |> Array.choose (fun x -> 
                if x.StartsWith "#" then
                    None
                else 
                    splitAtFirst ':' x                
                    |> fun (k,v) -> Some (k, Field v)
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
            let flags = arguments |> Array.choose (fun (k,v) -> match v.Arg with | Flag b -> Some (k,Flag b) | _ -> None) 
            let fields = 
                arguments 
                |> Array.choose (fun (k,v) -> match v.Arg with | Field b -> Some (k,v) | _ -> None) 
                |> createQuery editorPath arcPath serializeAnnotatedArguments deserializeArguments 
            Array.append flags fields
            |> Map.ofArray

        /// If parameters are missing a mandatory field, opens a textprompt containing the result of the serialized input parameters. Returns the deserialized user input
        let createArgumentQueryIfNecessary editorPath arcPath (arguments:(string*AnnotatedArgument) []) = 
            if isMissingMandatoryAttribute arguments then
                createArgumentQuery editorPath arcPath arguments
            else 
                arguments
                |> Array.map (fun (k,v) -> k,v.Arg)
                |> Map.ofArray

        /// Open a textprompt containing the serialized input item. Returns item updated with the deserialized user input
        let createItemQuery editorPath arcPath (item : #InvestigationFile.ISAItem) = 
            let serializeF inp = 
                InvestigationFile.getKeyValues inp
                |> Array.map (fun (k,v) -> sprintf "%s:%s" k v)
                |> Array.reduce (fun a b -> a + "\n" + b)
            let deserializeF (s:string) =
                s.Split '\n'
                |> Array.iter (fun x ->                 
                    splitAtFirst ':' x
                    |> System.Collections.Generic.KeyValuePair
                    |> fun kv -> InvestigationFile.setKeyValue kv item
                    |> ignore
                )

                item
            createQuery editorPath arcPath serializeF deserializeF item
