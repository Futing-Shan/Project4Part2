
open System.Text.Json.Serialization
open Newtonsoft.Json
open System
open System.Net
open System.IO
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
open System.Threading

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




let decodeRequst<'a> (req:HttpRequest) = 
    //printfn("in decode request")
    let getString rawForm = UTF8.toString rawForm
    //("rawForm:%A") req.rawForm
    JsonConvert.DeserializeObject(req.rawForm|>getString,typeof<'a>):?>'a


let MsgProcess=
    request(
        fun x ->
            let data = decodeRequst<String> x
            if(data.StartsWith("end")) then
                    printfn("====================Query End==========================")
                else
                    printfn("%A") data
            OK "success"   
    )

let app =

          choose [ 
            POST >=> choose[ path "/msg" >=>  MsgProcess] 
            NOT_FOUND "Found no handlers." ]

printfn("please enter your port:")
let port = Int32.Parse(Console.ReadLine())
let cfg =

          { defaultConfig with

              bindings = [ HttpBinding.createSimple HTTP "10.20.0.130" port]}

let cancellationTokenSource = new CancellationTokenSource ()
let config =  { cfg with cancellationToken = cancellationTokenSource.Token }
let _, webServer = startWebServerAsync config app
Async.Start (webServer, cancellationTokenSource.Token) |> ignore

//client
let mutable username = "add"
let mutable logoutFlag = false
let ipaddress = "http://10.20.0.209:8080"

let postDocRaw (url:string,data: string) : string =

    let request = WebRequest.Create(url)
    request.Method        <- "POST"
    request.ContentType   <- "application/json; charset=UTF-8"

      do(
        use wstream = request.GetRequestStream() 
        use sw = new StreamWriter(wstream)
        data|>JsonConvert.SerializeObject|>sw.Write
        )

      // todo：json再转一步string
      let response  = request.GetResponse()
      use reader = new StreamReader(response.GetResponseStream())
      let output= reader.ReadToEnd()
      output



let getDocRaw (url:string)  : string =

      let request = WebRequest.Create(url)
      request.Method        <- "GET"
     
      let response  = request.GetResponse()
      use reader     = new StreamReader(response.GetResponseStream())
      let output = reader.ReadToEnd()

      reader.Close()
      response.Close()
      request.Abort()

      output


let registerfun()




// printfn("response:%A" (postDocRaw "http://10.20.0.130:8080/register" "add")
// Console.ReadLine()|>ignore
