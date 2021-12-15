// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp
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
open System.Collections.Generic
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

              bindings = [ HttpBinding.createSimple HTTP "10.20.0.209" port]}

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
    use reader     = new StreamReader(response.GetResponseStream())
    let output = reader.ReadToEnd()

    output

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

//用户注册
let registerProcess=
    request(
        fun x ->
            printfn("x:= %A") x    
            let msg = decodeRequst<String> x
            printfn("decode data:%A") msg
            let username=(msg.ToString().Split('@')).[0]
            let portnumber=(msg.ToString().Split('@')).[1]
            user_port.Add(username,portnumber)
            printfn("username:%s") username
            if(userlist.ContainsKey(username)) then //旧用户登陆
                userlist.Item(username) <- 1     
                printfn ("%s already exits,now login") username
                printfn "current user: %A" userlist
            else     //首次登陆
                userlist.Add(username, 1)
                printfn "user name: %s has been added sucessfully" (username)
                printfn "current user: %A" userlist
            //let portnumber = user_port.Item(username).ToString()
            printfn("portnum:%s") portnumber
            //postDocRaw("http://10.20.0.130:"+portnumber+"/msg","successlogin")|>ignore
            OK "successlogin"   
    )

//用户退出
let logout = 
    request(
      fun x ->
          printfn("x:= %A") x
          let msg = decodeRequst<String> x
          printfn("decode data:%A") msg
          let username = msg.ToString()
          userlist.Item(username) <- 0
          user_port.Remove(username) |>ignore
          printfn "%A user logged out" username
          OK "successlogout" 
    )



let mutable temp_twitter = [] //工具人，没啥用，不能删。
let mutable temp_tag = [] //工具人2，没啥用，不能删。
let mutable temp_mention :list<string> = [] //工具人2，没啥用，不能删。
let user_twitter = new Dictionary<string,list<string>>()// 根据用户名生成字典，存储 “XXX” 发的所有推特
let all_twitter = new Dictionary<int,string>() // 把所有推特存储到一个字典里，给上编号。
let mutable index_twitter = 1
let subscribe = new Dictionary<string,list<string>>()// 根据用户名生成关注列表，存储“XXXX”的所有关注
let follower = new Dictionary<string,list<string>>() // 根据用户名生成粉丝列表，存储“XXX”的所有粉丝
//关注处理函数块
let mutable ifexits = true
//subscribe
let subscribe_user = 
    request(
      fun x ->
        printfn("x:= %A") x
        let msg = decodeRequst<String> x
        printfn("decode data:%A") msg
        let message = msg.ToString().Split("@")
        let username = message.[0]
        let temp_subscribe = message.[1]
        printfn ("%s is trying follow %s") username temp_subscribe
        if  (subscribe.ContainsKey(username)) then //避免重复关注
            for i in subscribe.Item(username) do
                if (i=temp_subscribe) then
                    ifexits <- false
        if (ifexits) then        
            if  (subscribe.ContainsKey(username)) then// 如果已经有这个人了，那就直接更新他的关注列表
                subscribe.Item(username) <- (temp_subscribe :: subscribe.Item(username))
                //printfn (" %s has subscribed %s") username temp_subscribe
                //printfn ("below is subscribe dictionary: %A") subscribe
            elif (not (subscribe.ContainsKey(username))) then// 如果没有这个人了，那就新建
                subscribe.Add(username,temp_subscribe::[])
                //printfn (" %s firstly subscrib %s") username temp_subscribe
                //printfn ("below is subscribe dictionary: %A") subscribe
            if  (follower.ContainsKey(temp_subscribe)) then// 如果已经有这个人了，那就直接更新他的粉丝列表
                follower.Item(temp_subscribe) <- (username :: subscribe.Item(temp_subscribe))
                //printfn (" %s become a follower of %s") temp_subscribe username 
                //printfn ("below is follower dictionary: %A") follower
            elif (not (follower.ContainsKey(temp_subscribe))) then// 如果没有这个人了，那就新建
                follower.Add(temp_subscribe,username::[])
                //printfn (" %s become the first follower of %s") temp_subscribe username 
                //printfn ("below is follower dictionary: %A") follower
        elif (not(ifexits)) then
            printfn "%s has already subscribed %s" username temp_subscribe
        OK "successsubscribe"         
    )

// 负责维护正在“view”状态函数快 ：view
let mutable view_user =List()
let mutable view_index = 0
let view =
  request(
    fun x ->
      printfn("x:= %A") x
      let msg = decodeRequst<String> x
      printfn("decode data:%A") msg
      // 收到的消息的格式是： view@add 代表add目前进入了view状态  logoutview@add 代表add目前退出了view状态
      if (msg.ToString().StartsWith("view")) then
          let username = msg.ToString().Split("@").[1]
          view_user.Add(username) //将username 加入到view状态的list里头
          printfn "%s is in view status" username
      elif (msg.ToString().StartsWith("logoutview")) then//将username从view状态的list里头移除
          let username = msg.ToString().Split("@").[1]
          if (view_user.Contains(username)) then
              printfn ("%s is logout") username
              view_user.Remove(username) |>ignore
          elif (not(view_user.Contains(username))) then
              printfn ("%s is not in the viewed status, something must be wrong here") username
      OK "successview" 
    )



let mutable current_follower = []

let final_broadcast(msg:string)=
      printfn("decode data:%A") msg
      let username = msg.ToString().Split(":").[1]
      if (follower.ContainsKey(username)) then
          for i in view_user do
              current_follower <- i :: current_follower
              if (follower.ContainsValue(current_follower)) then
                  let mutable temp_s = msg.ToString().Split(":").[1]+":"
                  for i in msg.ToString().Split(":").[2..] do
                      temp_s <-temp_s+i+":"
                  temp_s.Remove(temp_s.Length-2) |> ignore
                  let portnumber = user_port.Item(i.ToString()).ToString()
                  postDocRaw("http://10.20.0.130:"+portnumber+"/msg",(msg.ToString().Split(":").[0]+"."+temp_s)) |>ignore//将add的消息返回给正在view状态的粉丝们
                  printfn("")
              current_follower <- []
      elif (not(follower.ContainsKey(username))) then
          printfn "%s does have any followers" username
      0  

let tag_twitter = new Dictionary<string,list<string>>()
let mention_twitter = new Dictionary<string,list<string>>()
// 负责处理推特的actor
let final_tweet(msg:string) = 
      let username = msg.ToString().Split("@").[0]
      //提取出推特的内容
      let mutable twitter_content = ""
      let mutable flag = false
      for i in msg.ToString() do
          if(flag) then
              twitter_content <- twitter_content + i.ToString()
          if(i.ToString()="@") then
              flag <- true
      //printfn ("the content is :%s") twitter_content
      // 把所有推特存储到一个字典里，给上编号。
      all_twitter.Add(index_twitter,username+":"+twitter_content)  
      //printfn ("current all twitter is: %A") all_twitter
      final_broadcast(index_twitter.ToString()+":"+username+":"+twitter_content) |>ignore //先将推特的内容转给负责分发的actor
      // 根据用户名生成字典，存储 “XXX” 发的所有推特
      if(user_twitter.ContainsKey(username)) then
          user_twitter.Item(username) <- (index_twitter.ToString()+":"+twitter_content :: user_twitter.Item(username))
          //printfn ("user: %s send a twitter, the content is : %A") username twitter_content
      elif (not (user_twitter.ContainsKey(username))) then
          temp_twitter <- twitter_content :: temp_twitter
          user_twitter.Add(username,temp_twitter)
          temp_twitter <- []

      //提取tag的内容和mention的内容,并且判断是否有mention部分和tag部分。
      let twitter = twitter_content
      let mutable i =0
      let mutable temptag = ""
      let mutable tempmention = ""
      let mutable iftag = false
      let mutable ifmention = false
      while (i<twitter.Length) do
          if (twitter.[i]='#') then
              i<-(i+1)
              temptag <- ""
              while(i<>twitter.Length && twitter.[i]<>' ') do
                  temptag<-temptag+(twitter.[i].ToString())
                  i<-(i+1)
              iftag <- true
          elif (twitter.[i]='@') then 
              i<-(i+1)
              tempmention <- ""
              while(i<>twitter.Length && twitter.[i]<>' ') do
                  tempmention<-tempmention+(twitter.[i].ToString())
                  i<-(i+1)
              ifmention <- true
          else 
              i<-i+1

      //根据tag生成字典，存储 “#XXXX” tag下的所有推文
      if(iftag) then
          if(tag_twitter.ContainsKey(temptag)) then
              tag_twitter.Item(temptag) <- (index_twitter.ToString()+":"+username+":"+twitter_content :: tag_twitter.Item(temptag))
          elif (not(tag_twitter.ContainsKey(temptag))) then
              temp_tag <- (index_twitter.ToString()+":"+username+":"+twitter_content) :: temp_tag
              tag_twitter.Add(temptag,temp_tag)
              temp_tag<- []

      //根据mention生成字典，存储 “@XXXX” mention下的所有推文
      if(ifmention) then
          if(mention_twitter.ContainsKey(tempmention)) then
              mention_twitter.Item(tempmention) <- (index_twitter.ToString()+":"+username+":"+twitter_content :: mention_twitter.Item(tempmention))
          elif (not(mention_twitter.ContainsKey(temptag))) then
              temp_mention <- (index_twitter.ToString()+":"+username+":"+twitter_content) :: temp_mention
              mention_twitter.Add(tempmention,temp_mention)
              temp_mention<- []
      index_twitter <- (index_twitter + 1)
      0 


  




let tweet =
  request(
    fun x ->
        printfn("x:= %A") x
        let msg = decodeRequst<String> x
        printfn("decode data:%A") msg
        final_tweet(msg.ToString()) |>ignore
        OK "successsend"  
  )


let retweet =
  request(
    fun x ->
        printfn("x:= %A") x
        let msg = decodeRequst<String> x
        printfn("decode data:%A") msg

        let username = msg.ToString().Split("@").[0]
        printfn "this is username for retweet actor %s" username
        let twino =  Int32.Parse(msg.ToString().Split("@").[1])
        let twi_content=all_twitter.Item(twino)
        printfn("%s") twi_content
        let tmp_msg=username+"@Retweet["+twi_content+"]"
        printfn("%s") tmp_msg
        printfn "this is msg reply to client %s" tmp_msg
        final_tweet(tmp_msg) |>ignore
        OK "successretweet" 
)


let query = 
  request(
    fun x ->
        printfn("x:= %A") x
        let msg = decodeRequst<String> x
        printfn("decode data:%A") msg

        if (msg.ToString().StartsWith("@")) then  
            printfn "entering  the mention query"
            let username = msg.ToString().Split("@").[1]
            let portnumber = user_port.Item(username).ToString()
            printfn "this is the usernmae: %s" username
            for i in mention_twitter.Item(username) do
                postDocRaw("http://10.20.0.130:"+portnumber+"/msg",i.ToString())|> ignore
            postDocRaw("http://10.20.0.130:"+portnumber+"/msg","end")|> ignore
        elif (msg.ToString().StartsWith("&")) then
            printfn "entering  the subscription query"
            let username=msg.ToString().Split("&").[1]
            let portnumber = user_port.Item(username).ToString()
            printfn "this is the usernmae: %s" username
            for j in subscribe.Item(username) do
                for i in user_twitter.Item(j) do
                    postDocRaw("http://10.20.0.130:"+portnumber+"/msg",i.ToString())|> ignore
            postDocRaw("http://10.20.0.130:"+portnumber+"/msg","end")|> ignore
            printfn("")
        else
            printfn "entering  the tag query"
            let content=msg.ToString().Split("#").[1]
            let username = msg.ToString().Split("#").[0]
            let portnumber = user_port.Item(username).ToString()
            printfn "this is the usernmae: %s and the content is : %s" username content
            for i in tag_twitter.Item(content) do
                postDocRaw("http://10.20.0.130:"+portnumber+"/msg",i.ToString())|> ignore
            postDocRaw("http://10.20.0.130:"+portnumber+"/msg","end")|> ignore
            printfn("")
        OK "successquery"       
  )




let app =
          choose [ 
            path "/websocket" >=> handShake ws
            GET >=> choose [ path "/" >=> request (fun _ -> OK "Hello World!")]
            POST >=> choose[ path "/register" >=> registerProcess
                             path "/logout" >=> logout
                             path "/subscribe" >=> subscribe_user
                             path "/tweet" >=> tweet
                             path "/retweet" >=> retweet
                             path "/view" >=> view
                             path "/query" >=> query ]
            NOT_FOUND "Found no handlers." ]


startWebServer cfg app

Console.ReadLine()|>ignore