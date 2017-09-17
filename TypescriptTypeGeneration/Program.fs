open TypescriptTypeGeneration

// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
open Newtonsoft.Json
open System.IO

[<EntryPoint>]
let main argv = 
    let allAvailableTypes = TypeProvider.getTypes (typeof<Library1.thingy>) 
    allAvailableTypes
        |> TypeProvider.getClasses
        |> Seq.collect (fun t -> ClassTypeProvider.getProperties t |> Seq.map (fun p -> (t.Name, p)))
        |> Seq.iter (fun (typeName, (propertyName, propertyType)) -> printfn "Class %s has property with name %s" typeName propertyName)

    allAvailableTypes
        |> TypeProvider.getEnums
        |> Seq.collect (fun enum -> EnumTypeProvider.getEnumValues enum |> Seq.map (fun nameValuePair -> (enum.Name, nameValuePair)))
        |> Seq.iter (fun (enumName, (enumValueName, enumValue)) -> printfn "Enum with name %s has value %s = %d" enumName enumValueName enumValue)

    allAvailableTypes
        |> TypeProvider.getClasses
        |> Seq.map (fun c -> JsonConvert.SerializeObject(c, new TypeJsonConverter.TypeJsonConverter()))
        |> Seq.map JsonConvert.DeserializeObject
        |> Seq.map (fun obj -> JsonConvert.SerializeObject(obj, Formatting.Indented))
        |> (fun json -> System.String.Join(",", json))
        |> (fun allJson -> File.WriteAllText("stuff.json", allJson))
    
    printfn "All done!"
    System.Console.Read() |> ignore
    0 // return an integer exit code
