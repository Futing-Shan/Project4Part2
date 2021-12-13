// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open Suave
open Suave.Operators
open Suave.Filters
open Suave.Successful


let port = 8080

let cfg =

          { defaultConfig with

              bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" port]}

let app =

          choose

            [ GET >=> choose

                [ path "/" >=> request (fun _ -> OK "Hello World!")]

            ]


startWebServer cfg app