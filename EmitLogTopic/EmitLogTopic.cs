using System.Text;
using RabbitMQ.Client;

var factory = new ConnectionFactory { HostName = "localhost" };     //连接 RabbitMQ 工厂实例

using var connection = factory.CreateConnection();                  //创建连接对象
using var channel = connection.CreateModel();                       //创建一个新的通道

//声明一个名为“topic_logs”的主题（topic）交换机
channel.ExchangeDeclare(exchange: "topic_logs", type: ExchangeType.Topic);

var routingKey = (args.Length > 0) ? args[0] : "anonymous.info";    //初始化日志路由键("<设备>.<严重程度>")，从命令行参数的第一个参数获取，如果命令行不带参数，则默认为 `anonymous.info`
var message = (args.Length > 1)                                     //初始化消息（从命令行参数的第二个参数开始获取，如果命令行参数小于二，则默认为"Hello World!"）
                    ? string.Join(" ", args.Skip(1).ToArray())
                    : "Hello World!";
var body = Encoding.UTF8.GetBytes(message);                         //UTF-8 编码得到字节数组
//发送日志消息
channel.BasicPublish(exchange: "topic_logs",                        //指定要将消息发布到“topic_logs”主题交换机
                     routingKey: routingKey,                        //路由键包含两个单词 "<设备>.<严重程度>"
                     basicProperties: null,                         //指定消息属性，可以用来设置消息的优先级、过期时间等属性
                     body: body);                                   //消息内容，将要发送的数据转换为字节数组，并传递给这个参数
Console.WriteLine($" [x] 发送 '{routingKey}':'{message}'");