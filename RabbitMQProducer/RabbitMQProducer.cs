// See https://aka.ms/new-console-template for more information

using RabbitMQ.Client;
using System.Text;

//连接 RabbitMQ 工厂实例
var factory = new ConnectionFactory() { HostName = "localhost",         //主机名，默认为 "localhost"
                                        Port = 5672,                    //要连接的端口，默认为 -1(5672)
                                        UserName = "guest",             //用户名，默认为 "guest"
                                        Password = "guest"};            //密码，默认为 "guest"
//创建连接对象
using (var connection = factory.CreateConnection())
//创建一个新的通道、会话和模型
using(var channel = connection.CreateModel())
{
    //声明一个消息队列
    channel.QueueDeclare(queue: "myTestQueue",      //消息队列名称
                         durable: false,            //队列是否能在代理重启后仍然存在
                         exclusive: false,          //是否应该将此队列限制在其声明的连接中?这样的队列将在其声明连接关闭时被删除。（在一个客户端同时发送和读取消息的应用场景中适用）
                         autoDelete: false,         //当最后一个消费者(如果有的话)退订时，是否应该自动删除这个队列?
                         arguments: null);          //可选的;额外的队列参数，例如:“x-queue-type”
    //定义消息内容
    var message = "Hello RabbitMQ!";
    //转换为字节数组（编码格式UTF-8）
    var body = Encoding.UTF8.GetBytes(message);
    Console.WriteLine("按任意键开始发送消息！");
    Console.ReadKey();
    //循环发送一百次消息
    for(int i = 0; i < 100; i++)
    {
        /*
         * 向 RabbitMQ 服务器发送一条消息
         * 调用 BasicPublish 方法后，指定的消息内容就会被发送到 RabbitMQ 服务器，并根据您指定的路由键被路由到相应的消息队列中
         */
        channel.BasicPublish(exchange: "",                  //指定要将消息发布到哪个交换机（exchange），设置为空字符串以使用默认交换机
                             routingKey: "myTestQueue",     //指定消息的路由键（routing key），路由键用于确定消息应该被发送到哪些消息队列
                             basicProperties: null,         //指定消息属性，可以用来设置消息的优先级、过期时间等属性
                             body: body);                   //消息内容，将要发送的数据转换为字节数组，并传递给这个参数
        //控制台打印发送的消息
        Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss fff")}] Send: {message}");
        //当前线程暂停一秒钟
        Thread.Sleep(1000);
    }
}