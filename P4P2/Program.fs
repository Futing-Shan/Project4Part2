// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open Suave
open Suave.Http
open Suave.Operators
open Suave.Filters
open Suave.Successful
open Suave.Files
open Suave.RequestErrors
open Suave.Logging
open Suave.Utils

open System
open System.Net

open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket


let port = 8080

let cfg =

          { defaultConfig with

              bindings = [ HttpBinding.createSimple HTTP "10.136.245.87" port]}


let ws (webSocket : WebSocket) (context: HttpContext) =
  socket {
    // if `loop` is set to false, the server will stop receiving messages
    let mutable loop = true

    while loop do
      // the server will wait for a message to be received without blocking the thread
      let! msg = webSocket.read()

      match msg with
      // the message has type (Opcode * byte [] * bool)
      //
      // Opcode type:
      //   type Opcode = Continuation | Text | Binary | Reserved | Close | Ping | Pong
      //
      // byte [] contains the actual message
      //
      // the last element is the FIN byte, explained later
      | (Text, data, true) ->
        // the message can be converted to a string
        let str = UTF8.toString data
        let response = sprintf "response to %s" str

        // the response needs to be converted to a ByteSegment
        let byteResponse =
          response
          |> System.Text.Encoding.ASCII.GetBytes
          |> ByteSegment

        // the `send` function sends a message back to the client
        do! webSocket.send Text byteResponse true

      | (Close, _, _) ->
        let emptyResponse = [||] |> ByteSegment
        do! webSocket.send Close emptyResponse true

        // after sending a Close message, stop the loop
        loop <- false

      | _ -> ()
    }




let app =

          choose [ 
            path "/websocket" >=> handShake ws
            GET >=> choose [ path "/" >=> request (fun _ -> OK "Hello World!")]
            POST >=> choose[ path "/hello" >=> OK "Hello POST"
                             path "/goodbye" >=> OK "Good bye POST" ] 
            NOT_FOUND "Found no handlers." ]


startWebServer cfg app