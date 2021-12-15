
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


let viewLiveTwitter()=
    printfn("View Live Twitter. (Press Enter to escape)")
    let msg = "view@"+username
    postDocRaw(ipaddress+"/view",msg)|>ignore
    while(Console.ReadKey().KeyChar.ToString().Equals("\n")) do
        0|>ignore
    let logoutmsg = "logoutview@"+username
    postDocRaw(ipaddress+"/view",msg)|>ignore    
    printfn("Exit Live Twitter")

let sendTwitter()=
    printfn("Please enter your twitter. (#tag @mention,don't miss the space!)")
    let twitter_msg = Console.ReadLine() 
    let msg = username+"@"+twitter_msg
    //printfn("twitterMsg:%s") msg
    postDocRaw(ipaddress+"/tweet",msg)|>ignore

let reTweet()=
    printfn("Please enter the NO. of the twitter want to retweet.")
    let twitter_no = Console.ReadLine()
    let msg = username+"@"+twitter_no
    printfn("retweetMsg:%s") msg
    postDocRaw(ipaddress+"/retweet",msg)|>ignore
    0|>ignore

let queryTwitter()=
    printfn("Please do the selection.")
    printfn("===================================")
    printfn("1.Tag")
    printfn("2.My Mention")
    printfn("3.My subscription")
    printfn("===================================")
    let mutable choice = ""
    let selection = Console.ReadLine()
    if selection.Equals("1") then
        printfn("Please enter the tag.")
        choice<-Console.ReadLine()
        let msg = username+"#"+choice
        //printfn("QueryMsg:%s") msg
        postDocRaw(ipaddress+"/query",msg)|>ignore
    elif selection.Equals("2") then
        let msg = "@"+username
        //printfn("QueryMsg:%s") msg
        postDocRaw(ipaddress+"/query",msg)|>ignore
    elif selection.Equals("3") then
        let msg = "&"+username
        //printfn("QueryMsg:%s") msg 
        postDocRaw(ipaddress+"/query",msg)|>ignore
    else 
        printfn("Please enter the right number.")
    0|>ignore
 
let subscribeUser()=
    printfn("Please enter the username you want to subscribe.")
    let subscribe_name = Console.ReadLine()
    let msg = username+"@"+subscribe_name
    postDocRaw(ipaddress+"/subscribe",msg)|>ignore
    //("subscribeMsg:%s") msg 
    0|>ignore

let logOut()=
    postDocRaw(ipaddress+"/logout",username)|>ignore
    logoutFlag <- true
    printfn("ALready Logout, Thank you!")


let mutable selection="1"
let secondPage()=
    printfn("===================================")
    printfn("0.view live twitter")
    printfn("1.send twitter")
    printfn("2.re-tweet")
    printfn("3.query twitter")
    printfn("4.subscribe user")
    printfn("5.logout")
    printfn("===================================")
    printfn("Please enter your selection.")
    selection <- Console.ReadLine()
    if(selection.Equals("0")) then
        viewLiveTwitter()
        0
    elif(selection.Equals("1")) then
        sendTwitter()
        0
    elif(selection.Equals("2")) then
        reTweet()
        0 
    elif(selection.Equals("3")) then
        queryTwitter()
        0
    elif(selection.Equals("4")) then
        subscribeUser()
        0
    elif(selection.Equals("5")) then
        logOut()
        
        0
    else 
        printfn("Please enter the right selection.")
        0


let register()=
      printfn("please enter your username to register/login")
      username<-Console.ReadLine()
      let res_login = postDocRaw(ipaddress+"/register",username+"@"+port.ToString())
      if (res_login.Equals("successlogin")) then
            while (logoutFlag<>true) do
                  secondPage()
      else
            printfn("register failed")
            logoutFlag<-true



register()


