namespace IsaXLSX.AssayFile

open IsaXLSX


type Unit = OntologyAnnotation

type MaterialType = OntologyAnnotation





type Extract =
    {
    Name : string
    Characteristics : (Value*Value) Option
    MaterialType : (Value*Value) Option
    Description : string
    }

type Entry =
    | Characteristics of Value*Value //Characteristics
    | Factor of Value*Value //Factor, Factor Value
    | Parameter of Value*Value //Parameter, Parameter Value
    | Process of string*Value //Protocol REF
    | AssayName of string*string