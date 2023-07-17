using System.Text;
using RabbitMQ.Client;

var factory = new ConnectionFactory { HostName = "localhost" };     //连接 RabbitMQ 工厂实例
using var connection = factory.CreateConnection();                  //创建连接对象
using var channel = connection.CreateModel();                       //创建一个新的通道

//声明一个名为“logs”的扇出（fanout）交换机
channel.ExchangeDeclare(exchange: "logs", type: ExchangeType.Fanout);

var message = GetMessage(args);                         //初始化消息
var body = Encoding.UTF8.GetBytes(message);             //UTF-8 编码得到字节数组
channel.BasicPublish(exchange: "logs",                  //指定要将消息发布到“logs”扇出交换机
                     routingKey: string.Empty,          //指定消息的路由键（routing key）为空字符串，路由键用于确定消息应该被发送到哪些消息队列
                     basicProperties: null,             //指定消息属性，可以用来设置消息的优先级、过期时间等属性
                     body: body);                       //消息内容，将要发送的数据转换为字节数组，并传递给这个参数
Console.WriteLine($" [x] 发送 {message}");

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();

//消息以命令行参数的形式传送
//如果命令行不携带任何参数，默认发送消息为 "info: Hello World!"
/**
 * 在 C# 中，`args` 通常是指 `Main` 方法中的命令行参数。`args` 是一个字符串数组，用来接收来自命令行的参数。它是一个可选项，不是必须的。
 * 例如，您可以在命令行中运行一个 C# 程序，并传递一些参数给它。这些参数将被存储在 `args` 数组中，您可以在程序中访问它们。
 */
static string GetMessage(string[] args)
{
    return ((args.Length > 0) ? string.Join(" ", args) : "info: Hello World!");
}