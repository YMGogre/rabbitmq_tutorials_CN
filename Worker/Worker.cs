using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var factory = new ConnectionFactory { HostName = "localhost" };     //连接 RabbitMQ 工厂实例
using var connection = factory.CreateConnection();                  //创建连接对象
using var channel = connection.CreateModel();                       //创建一个新的通道

//声明工作队列
channel.QueueDeclare(queue: "task_queue",       //消息队列名称
                     durable: true,             //队列是否能在代理重启后仍然存在
                     exclusive: false,          //是否为独占（排他）队列
                     autoDelete: false,         //当最后一个消费者(如果有的话)退订时，是否应该自动删除这个队列?
                     arguments: null);          //可选的;额外的队列参数，例如:“x-queue-type”

//限制未确认消息数量为 1
channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

Console.WriteLine(" [*] 等待消息中...");

//创建（将 IBasicConsumer 接口实现为事件的）EventingBasicConsumer 类对象并关联指定的通道
var consumer = new EventingBasicConsumer(channel);
//使用 Lambda 表达式注册事件处理程序并订阅 Received 事件
consumer.Received += (model, ea) =>
{
    byte[] body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($" [x] 收到 {message}");

    //模拟耗时任务
    int dots = message.Split('.').Length - 1;
    Thread.Sleep(dots * 1000);

    Console.WriteLine(" [x] 完成");

    //在这儿您也可以通过 ((EventingBasicConsumer)sender).Model 访问到 channel
    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
};
//启动一个基本的内容类消费者（在当前通道中监听特定消息队列，并进行消费）
channel.BasicConsume(queue: "task_queue",       //消息队列名称
                     autoAck: false,            //停用自动消息（acknowledgement）确认（使用手动消息确认）
                     consumer: consumer);       //指定用于接收消息的消费者对象

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();