namespace TypescriptTypeGeneration

open System;

module EnumTypeProvider =
    let public getEnumValues (enumType: Type): (string * int) seq =
        enumType.GetEnumValues() :?> int array
            |> Seq.zip (enumType.GetEnumNames())

