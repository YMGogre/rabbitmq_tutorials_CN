using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

#region 创建 RabbitMQ 工厂实例；建立连接、通道
var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();
# endregion

//声明 RPC 消息队列
channel.QueueDeclare(queue: "rpc_queue",    //消息队列名称
                     durable: false,        //队列是否能在代理重启后仍然存在
                     exclusive: false,      //是否为独占（排他）队列
                     autoDelete: false,     //当最后一个消费者(如果有的话)退订时，是否应该自动删除这个队列?
                     arguments: null);      //可选的;额外的队列参数，例如:“x-queue-type”

//配置 QoS 参数。BasicQos() 是 RabbitMQ 中的一个方法，用于设置消费者预取（Consumer Prefetch）。这个方法可以限制在消费时通道（或连接）上未确认消息的数量（也称为“预取计数”）
channel.BasicQos(prefetchSize: 0,           //表示消息队列服务器将传送的内容的最大量（以八位字节为单位），如果为 0 则表示没有限制
                 prefetchCount: 1,          //表示消息队列服务器将传送的最大消息数，如果为 0 则表示无限制
                 global: false);            //表示是否应用于整个通道（true）或每个消费者（false）

//创建（将 IBasicConsumer 接口实现为事件的）EventingBasicConsumer 类对象并关联指定的通道
var consumer = new EventingBasicConsumer(channel);

//启动一个基本的内容类消费者（在当前通道中监听特定消息队列，并进行消费）
channel.BasicConsume(queue: "rpc_queue",        //消息队列名称
                     autoAck: false,            //是否自动发送确认消息（acknowledgement）给消息队列服务器
                     consumer: consumer);       //指定用于接收消息的消费者对象
Console.WriteLine(" [x] 等待 RPC 请求中...");

//使用 Lambda 表达式注册事件处理程序并订阅 Received 事件
consumer.Received += (model, ea) =>
{

    string response = string.Empty;

    var body = ea.Body.ToArray();                       //获取请求消息字节数组
    #region 获取请求消息的 CorrelationId 并将其作为响应消息的 CorrelationId
    var props = ea.BasicProperties;                     
    var replyProps = channel.CreateBasicProperties();
    replyProps.CorrelationId = props.CorrelationId;
    #endregion

    try
    {
        var message = Encoding.UTF8.GetString(body);    //UTF-8 格式编码请求消息内容字符串
        int n = int.Parse(message);                     //解析为 int 类型
        Console.WriteLine($" [.] Fib({message})");
        response = Fib(n).ToString();                   //调用斐波那契函数并将结果赋值给响应消息
    }
    catch (Exception e)
    {
        Console.WriteLine($" [.] {e.Message}");
        response = string.Empty;
    }
    finally
    {
        var responseBytes = Encoding.UTF8.GetBytes(response);               //UTF-8 格式编码响应消息字节数组
        //发布响应消息
        channel.BasicPublish(exchange: string.Empty,                        //指定要将消息发布到哪个交换机（exchange），设置为空字符串以使用默认交换机
                             routingKey: props.ReplyTo,                     //指定消息的路由键（routing key），路由键用于确定消息应该被发送到哪些消息队列（发布到 ReplyTo 属性指定的消息队列）
                             basicProperties: replyProps,                   //指定响应消息的 CorrelationId 属性即请求消息的 CorrelationId
                             body: responseBytes);                          //消息内容，将要发送的数据转换为字节数组，并传递给这个参数
        //发送确认消息给 RabbitMQ 服务器。调用此回执方法后，消息会被 RabbitMQ 服务器删除
        channel.BasicAck(deliveryTag: ea.DeliveryTag,                       //表示消息的投递序号，每次消费消息或者消息重新投递后，deliveryTag 都会增加
                         multiple: false);                                  //表示是否批量确认
        Console.WriteLine(" Press [enter] to exit.");
    }
};

Console.ReadLine();

// 斐波那契函数（只假设有效的正整数输入）
// 不要指望这个方法适用于大的数字，它可能是最慢的递归实现
static int Fib(int n)
{
    if (n is 0 or 1)
    {
        return n;
    }

    return Fib(n - 1) + Fib(n - 2);
}