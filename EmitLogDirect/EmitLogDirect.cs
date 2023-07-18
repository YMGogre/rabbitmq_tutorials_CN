using System.Text;
using RabbitMQ.Client;

var factory = new ConnectionFactory { HostName = "localhost" };     //连接 RabbitMQ 工厂实例
using var connection = factory.CreateConnection();                  //创建连接对象
using var channel = connection.CreateModel();                       //创建一个新的通道

//声明一个名为“direct_logs”的直连（direct）交换机
channel.ExchangeDeclare(exchange: "direct_logs", type: ExchangeType.Direct);

var severity = (args.Length > 0) ? args[0] : "info";                //初始化日志 '严重程度'（从命令行参数的第一个参数获取，如果命令行不带参数，则默认为 `info`）
var message = (args.Length > 1)                                     //初始化消息（从命令行参数的第二个参数开始获取，如果命令行参数小于二，则默认为"Hello World!"）
                    ? string.Join(" ", args.Skip(1).ToArray())
                    : "Hello World!";
var body = Encoding.UTF8.GetBytes(message);                         //UTF-8 编码得到字节数组
//发送消息
channel.BasicPublish(exchange: "direct_logs",                       //指定要将消息发布到“direct_logs”直连交换机
                     routingKey: severity,                          //将日志的 '严重程度' 作为路由键
                     basicProperties: null,                         //指定消息属性，可以用来设置消息的优先级、过期时间等属性
                     body: body);                                   //消息内容，将要发送的数据转换为字节数组，并传递给这个参数
Console.WriteLine($" [x] 发送 '{severity}':'{message}'");

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();
