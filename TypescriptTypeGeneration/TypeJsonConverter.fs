namespace TypescriptTypeGeneration

open Newtonsoft.Json
open System

module TypeJsonConverter =
    open System
    open Newtonsoft.Json.Linq

    let propertiesToIgnore = ["DeclaringType"; "ReflectedType";"TypeHandle"; "TypeInitializer"; "UnderlyingSystemType"]
    let methodsToIgnore = ["Finalize"; "GetConstructors"; "GetElementType"; "GetEnumUnderlyingType"; "GetType"; "GetTypeCodeImpl"; "MakePointerType"; "MemberwiseClone"; "MakeByRefType"; "MakeArrayType" ]

    type TypeJsonConverter() =
        inherit JsonConverter()
        member this.typeToJson (writer: JsonWriter) (serializer: JsonSerializer) (t: Type): unit =
            writer.WriteRaw("{\"TypeInfo\": {")
            typeof<Type>.GetProperties()
                |> Seq.filter (fun p -> not (Seq.contains p.Name propertiesToIgnore)) 
                |> Seq.iter (fun p -> try
                                        let value = p.GetValue(t, null)
                                        writer.WriteRaw(sprintf " \"%s\": " p.Name)
                                        this.WriteJson(writer, value, serializer)
                                        writer.WriteRaw(", ") //Unfortunately always adds trailing comma.
                                      with _ -> ()
                )
            writer.WriteRaw("}, \"TypeInfoMethods\": {")

            typeof<Type>.GetMethods()
                |> Seq.filter (fun m -> not (Seq.contains m.Name methodsToIgnore)) 
                |> Seq.filter (fun m -> not m.IsSpecialName) 
                |> Seq.filter (fun m -> m.GetParameters().Length = 0)
                |> Seq.iter (fun m -> try
                                        let value = m.Invoke(t, null)
                                        writer.WriteRaw(sprintf " \"%s\": " m.Name)
                                        this.WriteJson(writer, value, serializer)
                                        writer.WriteRaw(", ") //Unfortunately always adds trailing comma.
                                      with _ -> ()
                )

            writer.WriteRaw("}}")
            
        override this.ReadJson(reader, objectType, existingValue, serializer) = 
            raise (new NotImplementedException())
        override this.WriteJson(writer, value, serializer) =
            match value with
                | null -> ()
                | _ -> let reflectedType = value.GetType()
                       if typeof<Type>.IsAssignableFrom(value.GetType()) then 
                            let t: Type = value :?> Type
                            t |> this.typeToJson writer serializer
                       else JToken.FromObject(value).WriteTo(writer)
        override this.CanConvert(objectType: Type) = objectType.FullName = "System.Type" || (objectType.BaseType <> null && this.CanConvert(objectType.BaseType))

