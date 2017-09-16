namespace TypescriptTypeGeneration

open System

module TypeProvider = 
    //Return the types we're interested in consuming.
    let public getTypes (typeFromAssembly: Type): Type seq =
        typeFromAssembly.Assembly.GetTypes()
            |> Seq.filter (fun x -> not (x.FullName.StartsWith("<Startup")))
    
    let public getClasses (typesToFilter: Type seq): Type seq =
        typesToFilter |> Seq.filter (fun t -> t.IsClass)
    
    let public getEnums (typesToFilter: Type seq): Type seq =
        typesToFilter |> Seq.filter (fun t -> t.IsEnum)