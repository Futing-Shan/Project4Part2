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
open Suave.Json

open System
open System.Net
open System
open System.Text
open System.Security.Cryptography
open System.Collections.Generic

open System.Diagnostics

open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket

open System.Text.Json.Serialization
open Newtonsoft.Json


let user_port = new Dictionary<string,string>()
let userlist = new Dictionary<string,int>()

let port = 8080

let cfg =

          { defaultConfig with

              bindings = [ HttpBinding.createSimple HTTP "10.20.0.130" port]}


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


let decodeRequst<'a> (req:HttpRequest) = 
    printfn("in decode request")
    let getString rawForm = UTF8.toString rawForm
    printfn("rawForm:%A") req.rawForm
    JsonConvert.DeserializeObject(req.rawForm|>getString,typeof<'a>):?>'a



let registerProcess=
    request(
        fun x ->
            printfn("x:= %A") x    
            let data = decodeRequst<String> x
            printfn("decode data:%A") data
            let username=data            // let username = msg.ToString().Substring(7)
            printfn("username:%s") username
            if(userlist.ContainsKey(username)) then //旧用户登陆
                userlist.Item(username) <- 1     
                printfn ("%s already exits,now login") username
                printfn "current user: %A" userlist
            else     //首次登陆
                userlist.Add(username, 1)
                printfn "user name: %s has been added sucessfully" (username)
                printfn "current user: %A" userlist
            OK "successlogin"   
    )



let app =

          choose [ 
            path "/websocket" >=> handShake ws
            GET >=> choose [ path "/" >=> request (fun _ -> OK "Hello World!")]
            POST >=> choose[ path "/register" >=> registerProcess
                             path "/goodbye" >=> OK "Good bye POST" ] 
            NOT_FOUND "Found no handlers." ]


startWebServer cfg app