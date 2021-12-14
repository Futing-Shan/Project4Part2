open System
open System.Text
open System.Security.Cryptography
open System.Collections.Generic

open System.Diagnostics


let user_port = new Dictionary<string,string>()
let userlist = new Dictionary<string,int>()

//register
let username=(msg.ToString().Split('@')).[1]
            let portnumber=(msg.ToString().Split('@')).[2]
            user_port.Add(username,portnumber)
            // let username = msg.ToString().Substring(7)
            if(userlist.ContainsKey(username)) then //旧用户登陆
                userlist.Item(username) <- 1
                let response = select ("akka.tcp://Server@10.136.157.195:"+portnumber+"2552/user/user"+username)  akkasystem
                response <! "successlogin"
                
                printfn ("%s already exits,now login") username
                printfn "current user: %A" userlist
            else     //首次登陆
                userlist.Add(username, 1)
                printfn "user name: %s has been added sucessfully" (username)
                printfn "current user: %A" userlist
                let response = select ("akka.tcp://Server@10.136.157.195:"+portnumber+"/user/user"+username)  akkasystem
                response <! "successlogin"
                
//logout
let username = msg.ToString().Substring(7)
            userlist.Item(username) <- 0
            //let response = select ("akka.tcp://Server@10.20.0.154:"+user_port.Item(username)+"/user/user"+username)  akkasystem
            //response <! "successlogout"
            user_port.Remove(username) |>ignore
            printfn " %s user logged out, now current user: %A" username userlist                
       


let mutable temp_twitter = [] //工具人，没啥用，不能删。
let mutable temp_tag = [] //工具人2，没啥用，不能删。
let mutable temp_mention :list<string> = [] //工具人2，没啥用，不能删。
let user_twitter = new Dictionary<string,list<string>>()// 根据用户名生成字典，存储 “XXX” 发的所有推特
let all_twitter = new Dictionary<int,string>() // 把所有推特存储到一个字典里，给上编号。
let mutable index_twitter = 1




//负责关注的actor
let subscribe = new Dictionary<string,list<string>>()// 根据用户名生成关注列表，存储“XXXX”的所有关注
let follower = new Dictionary<string,list<string>>() // 根据用户名生成粉丝列表，存储“XXX”的所有粉丝
let mutable ifexits = true
//subscriber
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
    
// 负责维护正在“view”状态的list
let mutable view_user =List()
let mutable view_index = 0
let view_actor = spawn akkasystem "viewactor" (fun mailbox ->
    let rec loop() = actor {
        let! msg = mailbox.Receive()

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

        return! loop() 
    }
    loop())

// 分发actor   消息的形式： source_user@message
let mutable current_follower = []
let broadcast_actor = spawn akkasystem "broadcastactor" (fun mailbox ->
    let rec loop() = actor {
        let! msg = mailbox.Receive()
        let username = msg.ToString().Split(":").[1]
        //printfn "this is username: %s" username
        //printfn "%s" (msg.ToString())
        if (follower.ContainsKey(username)) then
            // 如果add有粉丝，那么我把消息返回给在“view”状态的粉丝。
            for i in view_user do
                current_follower <- i :: current_follower
                if (follower.ContainsValue(current_follower)) then //从view_user开始遍历，如果当前遍历的用户名属于add的粉丝，那么将该消息返回给该粉丝
                    //printfn ("%s 's fans : %A is in viewed status") username current_follower
                    //printfn("%s") ("akka.tcp://Server@10.136.157.195:"+user_port.Item(i)+"/user/msg"+i)      
                    let deliver_msg = select ("akka.tcp://Server@10.136.157.195:"+user_port.Item(i)+"/user/msg"+i)  akkasystem //将add的消息返回给正在view状态的粉丝们
                    //let deliver_msg = select ("akka.tcp://Server@10.20.0.154:"+portnumber+"/user/user"+username)  akkasystem
                    let mutable temp_s = msg.ToString().Split(":").[1]+":"
                    for i in msg.ToString().Split(":").[2..] do
                        temp_s <-temp_s+i+":"
                    temp_s.Remove(temp_s.Length-2) |> ignore
                    deliver_msg <! (msg.ToString().Split(":").[0]+"."+temp_s)
                current_follower <- []
        elif (not(follower.ContainsKey(username))) then
            printfn "%s does have any followers" username
        return! loop() 
    }
    loop())



let tag_twitter = new Dictionary<string,list<string>>()
let mention_twitter = new Dictionary<string,list<string>>()
// 负责处理推特的actor
let tweet_actor = spawn akkasystem "tweetactor" (fun mailbox ->    
    let rec loop() = actor { 
        let! msg = mailbox.Receive()
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
        broadcast_actor<! (index_twitter.ToString()+":"+username+":"+twitter_content) //先将推特的内容转给负责分发的actor
        // 根据用户名生成字典，存储 “XXX” 发的所有推特
        if(user_twitter.ContainsKey(username)) then
            user_twitter.Item(username) <- (index_twitter.ToString()+":"+twitter_content :: user_twitter.Item(username))
            //printfn ("user: %s send a twitter, the content is : %A") username twitter_content
        elif (not (user_twitter.ContainsKey(username))) then
            temp_twitter <- twitter_content :: temp_twitter
            user_twitter.Add(username,temp_twitter)
            temp_twitter <- []
            //printfn ("user %s is a new user, his msg has been added, below is the current user_twitter") username
            //printfn ("%A") user_twitter



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
                //printfn ("the tag #%s has been post, the content is : %A") temptag twitter_content
            elif (not(tag_twitter.ContainsKey(temptag))) then
                temp_tag <- (index_twitter.ToString()+":"+username+":"+twitter_content) :: temp_tag
                tag_twitter.Add(temptag,temp_tag)
                temp_tag<- []
                //printfn ("#%s is a new tag, content has been added, below is the current tag_twitter") temptag
                //printfn ("%A") tag_twitter

        //根据mention生成字典，存储 “@XXXX” mention下的所有推文
        if(ifmention) then
            if(mention_twitter.ContainsKey(tempmention)) then
                mention_twitter.Item(tempmention) <- (index_twitter.ToString()+":"+username+":"+twitter_content :: mention_twitter.Item(tempmention))
                //printfn ("the mention @%s has been post, the content is : %A") tempmention twitter_content
            elif (not(mention_twitter.ContainsKey(temptag))) then
                temp_mention <- (index_twitter.ToString()+":"+username+":"+twitter_content) :: temp_mention
                mention_twitter.Add(tempmention,temp_mention)
                temp_mention<- []
                //printfn ("@%s is a new mention, content has been added, below is the current mention_twitter") tempmention
                //printfn ("%A") mention_twitter
        index_twitter <- (index_twitter + 1) 
        return! loop() 
    }
    loop())

let retweet_actor = spawn akkasystem "retweetactor" (fun mailbox ->
    let rec loop() = actor {
        let! msg = mailbox.Receive()
        printfn "this is retweet msg %s" (msg.ToString())
        let username = msg.ToString().Split("@").[0]
        printfn "this is username for retweet actor %s" username
        let twino =  Int32.Parse(msg.ToString().Split("@").[1])
        let twi_content=all_twitter.Item(twino)
        printfn("%s") twi_content
        let tmp_msg=username+"@Retweet["+twi_content+"]"
        printfn("%s") tmp_msg
        printfn "this is msg reply to client %s" tmp_msg
        tweet_actor<!tmp_msg
        return! loop() 
    }
    loop())

let query_actor = spawn akkasystem "queryactor" (fun mailbox ->
    let rec loop() = actor {
        let! msg = mailbox.Receive()
        printfn "this is the query msg:%s" (msg.ToString())
        if (msg.ToString().StartsWith("@")) then
            printfn "entering  the mention query"
            let username = msg.ToString().Split("@").[1]
            printfn "this is the usernmae: %s" username
            let deliver_msg = select ("akka.tcp://Server@10.136.157.195:"+user_port.Item(username)+"/user/msg"+username)  akkasystem
            for i in mention_twitter.Item(username) do
                deliver_msg <! (i.ToString())
            deliver_msg <! "end"
        elif (msg.ToString().StartsWith("&")) then
            printfn "entering  the subscription query"
            let username=msg.ToString().Split("&").[1]
            printfn "this is the usernmae: %s" username
            let deliver_msg = select ("akka.tcp://Server@10.136.157.195:"+user_port.Item(username)+"/user/msg"+username)  akkasystem
            for j in subscribe.Item(username) do
                for i in user_twitter.Item(j) do
                    deliver_msg <! (i.ToString())
            deliver_msg <! "end"
        else
            printfn "entering  the tag query"
            let content=msg.ToString().Split("#").[1]
            let username = msg.ToString().Split("#").[0]
            printfn "this is the usernmae: %s and the content is : %s" username content
            let deliver_msg = select ("akka.tcp://Server@10.136.157.195:"+user_port.Item(username)+"/user/msg"+username)  akkasystem
            for i in tag_twitter.Item(content) do
                deliver_msg <! (i.ToString())
            deliver_msg <! "end"
        return! loop() 
    }
    loop())


Console.ReadLine()|>ignore