namespace TypescriptTypeGeneration

open System;

module ClassTypeProvider =
    let public getProperties (typeToGetPropertiesFrom: Type): (string * Type) seq = 
        typeToGetPropertiesFrom.GetProperties()
            |> Seq.map (fun x -> (x.Name, x.PropertyType))


