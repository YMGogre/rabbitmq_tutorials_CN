using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

//连接 RabbitMQ 工厂实例
var factory = new ConnectionFactory() { HostName = "localhost",     //主机名，默认为 "localhost"
                                        Port = 5672,                //要连接的端口，默认为 -1(5672)
                                        UserName = "guest",         //用户名，默认为 "guest"
                                        Password = "guest"};        //密码，默认为 "guest"
//创建连接对象
var connection = factory.CreateConnection();
//创建一个新的通道、会话和模型
var channel = connection.CreateModel();
//声明一个消息队列
channel.QueueDeclare(queue: "hello",            //消息队列名称
                     durable: false,            //队列是否能在代理重启后仍然存在
                     exclusive: false,          //是否应该将此队列限制在其声明的连接中?这样的队列将在其声明连接关闭时被删除。（在一个客户端同时发送和读取消息的应用场景中适用）
                     autoDelete: false,         //当最后一个消费者(如果有的话)退订时，是否应该自动删除这个队列?
                     arguments: null);          //可选的;额外的队列参数，例如:“x-queue-type”

//创建（将 IBasicConsumer 接口实现为事件的）EventingBasicConsumer 类对象并关联指定的 channel
var consumer = new EventingBasicConsumer(channel);
/* 
 * 启动一个基本的内容类消费者（在当前通道中监听 hello 消息队列，并进行消费）
 * 调用 BasicConsume 方法后，您的应用程序就可以开始从指定的消息队列中接收消息了。
 * 当从消息队列中接收到一条消息时，消费者对象的 Received 事件会被触发，并执行相应的事件处理程序。
 */
channel.BasicConsume(queue: "hello",            //消息队列名称
                     autoAck: true,             //启用自动消息（acknowledgement）确认
                     consumer: consumer);       //指定用于接收消息的消费者对象

//使用 Lambda 表达式注册事件处理程序并订阅 Received 事件
consumer.Received += (sender, e) =>
{
    var body = e.Body.ToArray();                                                                        //获取消息字节数组
    var message = Encoding.UTF8.GetString(body);                                                        //UTF-8格式编码消息内容字符串
    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss fff")}] Consumer Received: {message},");      //控制台打印消息内容（您也可以在这里添加其他的消息处理代码）
};
//控制台等待读取输入以避免进程退出
Console.ReadLine();
//关闭连接以及它的所有通道
connection.Close();