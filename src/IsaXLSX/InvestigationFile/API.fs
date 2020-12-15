namespace IsaXLSX.InvestigationFile

module API =

    module TermSource = 

        let tryGetBy (predicate : TermSource -> bool) (investigation:Investigation) =
            investigation.OntologySourceReference
            |> List.tryFind (predicate) 

        let tryGet (termSource : TermSource) (investigation:Investigation) =
            tryGetBy ((=) termSource) investigation

        /// If an termSource with the given identfier exists in the investigation, returns it
        let tryGetByName (name : string) (investigation:Investigation) =
            tryGetBy (fun t -> t.Name = name)  investigation

        let exists (predicate : TermSource -> bool) (investigation:Investigation) =
            investigation.OntologySourceReference
            |> List.exists (predicate) 

        let contains (termSource : TermSource) (investigation:Investigation) =
            exists ((=) termSource) investigation

        /// If an termSource with the given identfier exists in the investigation , returns it
        let existsByName (name : string) (investigation:Investigation) =
            exists (fun t -> t.Name = name) investigation

        /// adds the given termSource to the investigation  
        let add (termSource : TermSource) (investigation:Investigation) =
            {investigation with OntologySourceReference = List.append investigation.OntologySourceReference [termSource]}

        /// If an termSource exists in the investigation for which the predicate returns true, updates it with the given termSource
        let tryUpdateBy (predicate : TermSource -> bool) (termSource : TermSource) (investigation:Investigation) =
            if exists predicate investigation then
                {investigation with OntologySourceReference = List.map (fun a -> if predicate a then termSource else a) investigation.OntologySourceReference}
            else 
                investigation

        /// If an termSource with the given name exists in the investigation, returns it
        let tryUpdateByName (termSource : TermSource) (investigation:Investigation) =
            tryUpdateBy (fun t -> t.Name = termSource.Name) termSource investigation

        let tryRemoveBy (predicate : TermSource -> bool) (investigation:Investigation) =
            if exists predicate investigation then
                {investigation with OntologySourceReference = List.filter (predicate >> not) investigation.OntologySourceReference}
            else 
                investigation

        let tryRemove (termSource : TermSource) (investigation:Investigation) =
            tryRemoveBy ((=) termSource) investigation

        let tryRemoveByName (name : string) (investigation : Investigation) = 
            tryRemoveBy (fun t -> t.Name = name)  investigation

    module Study =

        let tryGetBy (predicate : Study -> bool) (investigation:Investigation) =
            investigation.Studies
            |> List.tryFind (predicate) 

        let tryGet (study : Study) (investigation:Investigation) =
            tryGetBy ((=) study) investigation

        /// If an study with the given identfier exists in the investigation, returns it
        let tryGetByIdentifier (fileName : string) (investigation:Investigation) =
            tryGetBy (fun s -> s.StudyInfo.Identifier = fileName)  investigation

        let exists (predicate : Study -> bool) (investigation:Investigation) =
            investigation.Studies
            |> List.exists (predicate) 

        let contains (study : Study) (investigation:Investigation) =
            exists ((=) study) investigation

        /// If an study with the given identfier exists in the investigation , returns it
        let existsByIdentifier (fileName : string) (investigation:Investigation) =
            exists (fun s -> s.StudyInfo.Identifier = fileName) investigation

        /// adds the given study to the investigation  
        let add (study : Study) (investigation:Investigation) =
            {investigation with Studies = List.append investigation.Studies [study]}

        /// If an study exists in the investigation for which the predicate returns true, updates it with the given study
        let tryUpdateBy (predicate : Study -> bool) (study : Study) (investigation:Investigation) =
            if exists predicate investigation then
                {investigation with Studies = List.map (fun a -> if predicate a then study else a) investigation.Studies}
            else 
                investigation

        /// If an study with the given fileName exists in the investigation, returns it
        let tryUpdateByIdentifier (study : Study) (investigation:Investigation) =
            tryUpdateBy (fun s -> s.StudyInfo.Identifier = study.StudyInfo.Identifier) study investigation

        let tryRemoveBy (predicate : Study -> bool) (investigation:Investigation) =
            if exists predicate investigation then
                {investigation with Studies = List.filter (predicate >> not) investigation.Studies}
            else 
                investigation

        let tryRemove (study : Study) (investigation:Investigation) =
            tryRemoveBy ((=) study) investigation

        let tryRemoveByIdentifier (identifier : string) (investigation : Investigation) = 
            tryRemoveBy (fun s -> s.StudyInfo.Identifier = identifier)  investigation

    module Assay =  
  
        let tryGetBy (predicate : Assay -> bool) (study:Study) =
            study.Assays
            |> List.tryFind (predicate) 

        let tryGet (assay : Assay) (study:Study) =
            tryGetBy ((=) assay) study

        /// If an assay with the given identfier exists in the study exists, returns it
        let tryGetByFileName (fileName : string) (study:Study) =
            tryGetBy (fun a -> a.FileName = fileName) study

        let exists (predicate : Assay -> bool) (study:Study) =
            study.Assays
            |> List.exists (predicate) 

        let contains (assay : Assay) (study:Study) =
            exists ((=) assay) study

        /// If an assay with the given identfier exists in the study exists, returns it
        let existsByFileName (fileName : string) (study:Study) =
            exists (fun a -> a.FileName = fileName) study

        /// adds the given assay to the study  
        let add (assay : Assay) (study:Study) =
            {study with Assays = List.append study.Assays [assay]}

        /// If an assay exists in the study for which the predicate returns true, updates it with the given assay
        let tryUpdateBy (predicate : Assay -> bool) (assay : Assay) (study:Study) =
            if exists predicate study then
                {study with Assays = List.map (fun a -> if predicate a then assay else a) study.Assays}
            else 
                study

        /// If an assay with the given fileName exists in the study exists, returns it
        let tryUpdateByFileName (assay : Assay) (study:Study) =
            tryUpdateBy (fun a -> a.FileName = assay.FileName) assay study

        let tryRemoveBy (predicate : Assay -> bool) (study:Study) =
            if exists predicate study then
                {study with Assays = List.filter (predicate >> not) study.Assays}
            else 
                study

        let tryRemove (assay : Assay) (study:Study) =
            tryRemoveBy ((=) assay) study

        let tryRemoveByFileName (fileName : string) (study : Study) = 
            tryRemoveBy (fun a -> a.FileName = fileName) study

        ///// TODO Filename = identfier ausformulieren
        ///// If no assay with the same identifier as the given assay exists in the study, adds the given assay to the study
        //let tryAdd (assay : Assay) (study:Study) =
        //    if study.Assays |> List.exists (fun studyAssay -> studyAssay.FileName = assay.FileName) then
        //        None
        //    else 
        //        Some {study with Assays = List.append study.Assays [assay]}

        ///// If an assay with the given identfier exists in the study exists, removes it from the study
        //let tryRemove (assayIdentfier : string) (study:Study) =
        //    if study.Assays |> List.exists (fun assay -> assay.FileName = assayIdentfier) then
        //        Some {study with Assays = List.filter (fun assay -> assay.FileName <> assaAssayyIdentfier) study.Assays }
        //    else 
        //        None

        ///// If an assay with the same identifier as the given assay exists in the study, overwrites its values with values in the given study
        //let tryUpdate (assay : Assay) (study:Study) =
        //    if study.Assays |> List.exists (fun studyAssay -> studyAssay.FileName = assay.FileName) then
        //        Some {study with Assays = 
        //                            study.Assays 
        //                            |> List.map (fun studyAssay -> if studyAssay.FileName = assay.FileName then assay else studyAssay) 
        //        }
        //    else 
        //        None

    module Protocol =  
  
        let tryGetBy (predicate : Protocol -> bool) (study:Study) =
            study.Protocols
            |> List.tryFind (predicate) 

        let tryGet (protocol : Protocol) (study:Study) =
            tryGetBy ((=) protocol) study

        /// If an protocol with the given identfier exists in the study exists, returns it
        let tryGetByName (name : string) (study:Study) =
            tryGetBy (fun p -> p.Name = name) study

        let exists (predicate : Protocol -> bool) (study:Study) =
            study.Protocols
            |> List.exists (predicate) 

        let contains (protocol : Protocol) (study:Study) =
            exists ((=) protocol) study

        /// If an protocol with the given identfier exists in the study exists, returns it
        let existsByName (name : string) (study:Study) =
            exists (fun p -> p.Name = name) study

        /// adds the given protocol to the study  
        let add (protocol : Protocol) (study:Study) =
            {study with Protocols = List.append study.Protocols [protocol]}

        /// If an protocol exists in the study for which the predicate returns true, updates it with the given protocol
        let tryUpdateBy (predicate : Protocol -> bool) (protocol : Protocol) (study:Study) =
            if exists predicate study then
                {study with Protocols = List.map (fun a -> if predicate a then protocol else a) study.Protocols}
            else 
                study

        /// If an protocol with the given name exists in the study exists, returns it
        let tryUpdateByName (protocol : Protocol) (study:Study) =
            tryUpdateBy (fun p -> p.Name = protocol.Name) protocol study

        let tryRemoveBy (predicate : Protocol -> bool) (study:Study) =
            if exists predicate study then
                {study with Protocols = List.filter (predicate >> not) study.Protocols}
            else 
                study

        let tryRemove (protocol : Protocol) (study:Study) =
            tryRemoveBy ((=) protocol) study


        let tryRemoveByName (name : string) (study : Study) = 
            tryRemoveBy (fun p -> p.Name = name) study

    module Factor =  
  
        let tryGetBy (predicate : Factor -> bool) (study:Study) =
            study.Factors
            |> List.tryFind (predicate) 

        let tryGet (factor : Factor) (study:Study) =
            tryGetBy ((=) factor) study

        /// If an factor with the given identfier exists in the study exists, returns it
        let tryGetByName (name : string) (study:Study) =
            tryGetBy (fun f -> f.Name = name) study

        let exists (predicate : Factor -> bool) (study:Study) =
            study.Factors
            |> List.exists (predicate) 

        let contains (factor : Factor) (study:Study) =
            exists ((=) factor) study

        /// If an factor with the given identfier exists in the study exists, returns it
        let existsByName (name : string) (study:Study) =
            exists (fun f -> f.Name = name) study

        /// adds the given factor to the study  
        let add (factor : Factor) (study:Study) =
            {study with Factors = List.append study.Factors [factor]}

        /// If an factor exists in the study for which the predicate returns true, updates it with the given factor
        let tryUpdateBy (predicate : Factor -> bool) (factor : Factor) (study:Study) =
            if exists predicate study then
                {study with Factors = List.map (fun f -> if predicate f then factor else f) study.Factors}
            else 
                study

        /// If an factor with the given name exists in the study exists, returns it
        let tryUpdateByName (factor : Factor) (study:Study) =
            tryUpdateBy (fun f -> f.Name = factor.Name) factor study

        let tryRemoveBy (predicate : Factor -> bool) (study:Study) =
            if exists predicate study then
                {study with Factors = List.filter (predicate >> not) study.Factors}
            else 
                study

        let tryRemove (factor : Factor) (study:Study) =
            tryRemoveBy ((=) factor) study


        let tryRemoveByName (name : string) (study : Study) = 
            tryRemoveBy (fun f -> f.Name = name) study

    module Person =  
  
        let tryGetBy (predicate : Person -> bool) (study:Study) =
            study.Contacts
            |> List.tryFind (predicate) 

        let tryGet (person : Person) (study:Study) =
            tryGetBy ((=) person) study

        /// If an person with the given identfier exists in the study exists, returns it
        let tryGetByFullName (firstName : string) (midInitials : string) (lastName : string) (study:Study) =
            tryGetBy (fun p -> p.FirstName = firstName && p.MidInitials = midInitials && p.LastName = lastName) study

        let exists (predicate : Person -> bool) (study:Study) =
            study.Contacts
            |> List.exists (predicate) 

        let contains (person : Person) (study:Study) =
            exists ((=) person) study

        /// If an person with the given identfier exists in the study exists, returns it
        let existsFullName (firstName : string) (midInitials : string) (lastName : string) (study:Study) =
            exists (fun p -> p.FirstName = firstName && p.MidInitials = midInitials && p.LastName = lastName) study

        /// adds the given person to the study  
        let add (person : Person) (study:Study) =
            {study with Contacts = List.append study.Contacts [person]}

        /// If an person exists in the study for which the predicate returns true, updates it with the given person
        let tryUpdateBy (predicate : Person -> bool) (person : Person) (study:Study) =
            if exists predicate study then
                {study with Contacts = List.map (fun f -> if predicate f then person else f) study.Contacts}
            else 
                study

        /// If an person with the given name exists in the study exists, returns it
        let tryUpdateByFullName (person : Person) (study:Study) =
            tryUpdateBy (fun p -> p.FirstName = person.FirstName && p.MidInitials = person.MidInitials && p.LastName = person.LastName) person study

        let tryRemoveBy (predicate : Person -> bool) (study:Study) =
            if exists predicate study then
                {study with Contacts = List.filter (predicate >> not) study.Contacts}
            else 
                study

        let tryRemove (person : Person) (study:Study) =
            tryRemoveBy ((=) person) study

        let tryRemoveByFullName (firstName : string) (midInitials : string) (lastName : string) (study:Study) =
            tryRemoveBy (fun p -> p.FirstName = firstName && p.MidInitials = midInitials && p.LastName = lastName) study

    module Design =  
  
        let tryGetBy (predicate : Design -> bool) (study:Study) =
            study.DesignDescriptors
            |> List.tryFind (predicate) 

        let tryGet (design : Design) (study:Study) =
            tryGetBy ((=) design) study

        /// If an design with the given identfier exists in the study exists, returns it
        let tryGetByDesignType (designType : string) (study:Study) =
            tryGetBy (fun d -> d.DesignType = designType) study

        let exists (predicate : Design -> bool) (study:Study) =
            study.DesignDescriptors
            |> List.exists (predicate) 

        let contains (design : Design) (study:Study) =
            exists ((=) design) study

        /// If an design with the given identfier exists in the study exists, returns it
        let existsByDesignType (designType : string) (study:Study) =
            exists (fun d -> d.DesignType = designType) study

        /// adds the given design to the study  
        let add (design : Design) (study:Study) =
            {study with DesignDescriptors = List.append study.DesignDescriptors [design]}

        /// If an design exists in the study for which the predicate returns true, updates it with the given design
        let tryUpdateBy (predicate : Design -> bool) (design : Design) (study:Study) =
            if exists predicate study then
                {study with DesignDescriptors = List.map (fun d -> if predicate d then design else d) study.DesignDescriptors}
            else 
                study

        /// If an design with the given name exists in the study exists, returns it
        let tryUpdateByDesignType (design : Design) (study:Study) =
            tryUpdateBy (fun f -> f.DesignType = design.DesignType) design study

        let tryRemoveBy (predicate : Design -> bool) (study:Study) =
            if exists predicate study then
                {study with DesignDescriptors = List.filter (predicate >> not) study.DesignDescriptors}
            else 
                study

        let tryRemove (design : Design) (study:Study) =
            tryRemoveBy ((=) design) study


        let tryRemoveByDesignType (designType : string) (study : Study) = 
            tryRemoveBy (fun d -> d.DesignType = designType) study

    module Publication =  
  
        let tryGetBy (predicate : Publication -> bool) (study:Study) =
            study.Publications
            |> List.tryFind (predicate) 

        let tryGet (publication : Publication) (study:Study) =
            tryGetBy ((=) publication) study

        /// If an publication with the given identfier exists in the study exists, returns it
        let tryGetByDOI (doi : string) (study:Study) =
            tryGetBy (fun p -> p.DOI = doi) study

        
        /// If an publication with the given identfier exists in the study exists, returns it
        let tryGetByPubMedID (pubMedID : string) (study:Study) =
            tryGetBy (fun p -> p.PubMedID = pubMedID) study

        let exists (predicate : Publication -> bool) (study:Study) =
            study.Publications
            |> List.exists (predicate) 

        let contains (publication : Publication) (study:Study) =
            exists ((=) publication) study

        /// If an publication with the given identfier exists in the study exists, returns it
        let existsByDoi (doi : string) (study:Study) =
            exists (fun p -> p.DOI = doi) study
        
        /// If an publication with the given identfier exists in the study exists, returns it
        let existsByPubMedID (pubMedID : string) (study:Study) =
            exists (fun p -> p.PubMedID = pubMedID) study

        /// adds the given publication to the study  
        let add (publication : Publication) (study:Study) =
            {study with Publications = List.append study.Publications [publication]}

        /// If an publication exists in the study for which the predicate returns true, updates it with the given publication
        let tryUpdateBy (predicate : Publication -> bool) (publication : Publication) (study:Study) =
            if exists predicate study then
                {study with Publications = List.map (fun p -> if predicate p then publication else p) study.Publications}
            else 
                study

        /// If an publication with the given name exists in the study exists, returns it
        let tryUpdateByDoi (publication : Publication) (study:Study) =
            tryUpdateBy (fun p -> p.DOI = publication.DOI) publication study

        /// If an publication with the given name exists in the study exists, returns it
        let tryUpdateByPubMedID (publication : Publication) (study:Study) =
            tryUpdateBy (fun p -> p.PubMedID = publication.PubMedID) publication study

        let tryRemoveBy (predicate : Publication -> bool) (study:Study) =
            if exists predicate study then
                {study with Publications = List.filter (predicate >> not) study.Publications}
            else 
                study

        let tryRemove (publication : Publication) (study:Study) =
            tryRemoveBy ((=) publication) study


        let tryRemoveByDoi (doi : string) (study : Study) = 
            tryRemoveBy (fun p -> p.DOI = doi) study

        
        let tryRemoveByPubMedID (pubMedID : string) (study : Study) = 
            tryRemoveBy (fun p -> p.PubMedID = pubMedID) study


