namespace ArcCommander

open System

open System.Reflection
open Microsoft.FSharp.Reflection

open System.Diagnostics
open YamlDotNet.Serialization;

open ISA.DataModel

type NotRequiredAttribute() =    
    inherit Attribute()

module ArgumentQuery = 
    
    module Assay = 

        type IAssay =
            abstract ToAssay : unit -> InvestigationFile.Assay

        [<CLIMutable>]
        type AssayMove = 
            { 
                AssayIdentifier : string
                StudyIdentifier : string
                TargetStudyIdentifier : string 
            }
            interface IAssay with
                member this.ToAssay() = InvestigationFile.Assay(fileName = this.AssayIdentifier)


        [<CLIMutable>]
        type AssayBasic = 
            {
                AssayIdentifier : string
                StudyIdentifier : string
            }
            interface IAssay with
                member this.ToAssay() = InvestigationFile.Assay(fileName = this.AssayIdentifier)

        [<CLIMutable>]
        type AssayFull = 
            {
                AssayIdentifier                       : string
                StudyIdentifier                       : string
                [<NotRequired>]  
                MeasurementType                       : string
                [<NotRequired>]  
                MeasurementTypeTermAccessionNumber    : string
                [<NotRequired>]  
                MeasurementTypeTermSourceREF          : string
                [<NotRequired>]  
                TechnologyType                        : string
                [<NotRequired>]  
                TechnologyTypeTermAccessionNumber     : string
                [<NotRequired>]  
                TechnologyTypeTermSourceREF           : string
                [<NotRequired>]  
                TechnologyPlatform                    : string
            }
            interface IAssay with
                member this.ToAssay() = 
                    InvestigationFile.Assay(
                        this.MeasurementType,
                        this.MeasurementTypeTermAccessionNumber,
                        this.MeasurementTypeTermSourceREF,
                        this.TechnologyType,
                        this.TechnologyTypeTermAccessionNumber,
                        this.TechnologyTypeTermSourceREF,
                        this.TechnologyTypeTermSourceREF,
                        this.AssayIdentifier                      
                )

        let toISAAssay (assay : #IAssay) =
            assay.ToAssay()

    /// Returns given attribute from property info as optional 
    let private tryGetCustomAttribute<'a> (findAncestor:bool) (propInfo :PropertyInfo) =   
        let attributeType = typeof<'a>
        let attrib = propInfo.GetCustomAttribute(attributeType, findAncestor)
        match box attrib with
        | (:? 'a) as customAttribute -> Some(unbox<'a> customAttribute)
        | _ -> None

    /// Returns given attribute from property info as optional 
    let private getAllRequiredValues (record:'T) =
      let schemaType = typeof<'T>
      let fields = FSharpType.GetRecordFields(schemaType)

      fields 
      |> Array.choose ( fun field -> 
          match tryGetCustomAttribute<NotRequiredAttribute> true field with 
          | Some x -> None
          | None   -> FSharpValue.GetRecordField (record,field) |> Some 
          )
        
    let internal containsAllRequiredValues (record:'T) =
        getAllRequiredValues record
        |> Array.contains (box null)
        |> not
      
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
        
    let askForFillout (editorPath) (arcPath) (item:'T) =
        let serializer = SerializerBuilder().Build();
        let deserializer = DeserializerBuilder().Build();
         
        let yamlString = serializer.Serialize(item);
        let hash = MD5Hash yamlString
        let filePath = sprintf "%s/.arc/%s.txt" arcPath hash
    
        write filePath yamlString
        try
        runProcess editorPath filePath
        read filePath
        |> deserializer.Deserialize<'T>

        with
        | err -> 
            failwithf "could not read query: %s" err.Message
    
    let askForFilloutIfNeeded (editorPath) (arcPath) (item:'T) =
        printfn "initial input: %O" item
        if containsAllRequiredValues item then
            printfn "is complete"
            item
        else 
            let inp = askForFillout editorPath arcPath item
            printfn "after querying input: %O" item
            inp