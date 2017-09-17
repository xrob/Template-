namespace TypescriptTypeGeneration

open System
open System.Reflection
open System.Linq
open System.Collections
open Newtonsoft.Json
open Newtonsoft.Json.Linq

module TypeJsonConverter =


    //todo: Need to add ParameterInfo to the list of supported types here?
    let propertiesToIgnore = [ 
                                (typeof<Type>, ["DeclaringType"; "ReflectedType";"TypeHandle"; "TypeInitializer"; "UnderlyingSystemType"]);
                                (typeof<PropertyInfo>, ["DeclaringType"; "ReflectedType";]);
                                (typeof<MethodInfo>, ["DeclaringType"; "ReflectedType";]);
                             ] |> dict
    let methodsToIgnore = [
                            (typeof<Type>, ["Finalize"; "GetConstructors"; "GetElementType"; "GetEnumUnderlyingType"; "GetType"; "GetTypeCodeImpl"; "MakePointerType"; "MemberwiseClone"; "MakeByRefType"; "MakeArrayType" ]);
                            (typeof<PropertyInfo>, ["Finalize";"GetAccessors";"GetGetMethod";"GetSetMethod";"GetType";"MemberwiseClone";]);
                            (typeof<MethodInfo>, ["Finalize";"GetType";"GetBaseDefinition";"MemberwiseClone";"GetGenericMethodDefinition";]);
                          ] |> dict

    //A special JSON converter (using Json.Net framework) which outputs the important information from type classes
    type TypeJsonConverter() =
        inherit JsonConverter()
        //Takes an objectToJsonify and writes to the JSON stream with more indepth information that JSON.net usually provides.
        //It does this by reflecting over the object and pulling out all the properties it thinks it can get away with.
        member this.toVerboseJson<'a> (writer: JsonWriter) (serializer: JsonSerializer) (objectToJsonify: 'a) : unit =
            let typeToReflectOver = typeof<'a>
            writer.WriteRaw("{")
            typeToReflectOver.GetProperties()
                |> Seq.filter (fun p -> not (Seq.contains p.Name propertiesToIgnore.[typeToReflectOver])) 
                |> Seq.iter (fun p -> try
                                        let value = p.GetValue(objectToJsonify, null)
                                        writer.WriteRaw(sprintf " \"%s\": " p.Name)
                                        this.WriteJson(writer, value, serializer)
                                        writer.WriteRaw(", ") //Unfortunately always adds trailing comma.
                                      with _ -> ()
                )

            typeToReflectOver.GetMethods()
                |> Seq.filter (fun m -> not (Seq.contains m.Name methodsToIgnore.[typeToReflectOver])) 
                |> Seq.filter (fun m -> not m.IsSpecialName) 
                |> Seq.filter (fun m -> m.GetParameters().Length = 0)
                |> Seq.iter (fun m -> try
                                        let value = m.Invoke(objectToJsonify, null)
                                        writer.WriteRaw(sprintf " \"%s\": " m.Name)
                                        this.WriteJson(writer, value, serializer)
                                        writer.WriteRaw(", ") //Unfortunately always adds trailing comma.
                                      with _ -> ()
                )

            writer.WriteRaw("}")
            
        override this.ReadJson(reader, objectType, existingValue, serializer) = 
            raise (new NotImplementedException())
        override this.WriteJson(writer, value, serializer) =
            match value with
                | null -> ()
                //If we can enumerate an object then we should generally serialize it as a list unless it's a value type or a string.
                | :? IEnumerable as list when (let typeOfValue = value.GetType() in not(typeOfValue.IsValueType || typeOfValue.FullName = typeof<string>.FullName)) -> 
                    writer.WriteRaw("[")
                    for item in list do 
                        this.WriteJson(writer, item, serializer)
                        writer.WriteRawValue(", ")
                    writer.WriteRaw("]")
                | :? Type as t when (not (t.IsPrimitive || t.Namespace.StartsWith("System"))) -> this.toVerboseJson writer serializer t
                | :? PropertyInfo as p -> this.toVerboseJson writer serializer p
                | :? MethodInfo as m-> this.toVerboseJson writer serializer m
                | _ -> JToken.FromObject(value).WriteTo(writer)
        override this.CanConvert(objectType: Type) = objectType.FullName = typeof<Type>.FullName || (objectType.BaseType <> null && this.CanConvert(objectType.BaseType))

