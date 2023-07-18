using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var factory = new ConnectionFactory { HostName = "localhost" };     //连接 RabbitMQ 工厂实例
using var connection = factory.CreateConnection();                  //创建连接对象
using var channel = connection.CreateModel();                       //创建一个新的通道

//声明一个名为“direct_logs”的直连（direct）交换机
channel.ExchangeDeclare(exchange: "direct_logs", type: ExchangeType.Direct);
//声明一个由 RabbitMQ 服务器分配名称的队列，默认为非持久存储的、独占（排他）的、自动删除的队列，获取队列名称
var queueName = channel.QueueDeclare().QueueName;

//绑定键由命令行参数提供，如果命令行不带参数，则报错
if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: {0} [info] [warning] [error]",
                            Environment.GetCommandLineArgs()[0]);
    Console.WriteLine(" Press [enter] to exit.");
    Console.ReadLine();
    Environment.ExitCode = 1;
    return;
}

//为每个 '严重程度' 创建新的绑定
foreach (var severity in args)
{
    //将队列绑定到交换机
    channel.QueueBind(queue: queueName,             //队列名称
                      exchange: "direct_logs",      //交换机名称
                      routingKey: severity);        //绑定键
}

Console.WriteLine(" [*] 等待消息中...");

//创建（将 IBasicConsumer 接口实现为事件的）EventingBasicConsumer 类对象并关联指定的通道
var consumer = new EventingBasicConsumer(channel);
//使用 Lambda 表达式注册事件处理程序并订阅 Received 事件
consumer.Received += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    var routingKey = ea.RoutingKey;
    Console.WriteLine($" [x] 收到 '{routingKey}':'{message}'");
};
//启动一个基本的内容类消费者（在当前通道中监听特定消息队列，并进行消费）
channel.BasicConsume(queue: queueName,          //消息队列名称
                     autoAck: true,             //启用自动消息（acknowledgement）确认
                     consumer: consumer);       //指定用于接收消息的消费者对象

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();