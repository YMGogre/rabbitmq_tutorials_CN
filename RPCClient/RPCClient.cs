using System.Collections.Concurrent;
using System.Runtime.Intrinsics.X86;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class RpcClient : IDisposable
{
    private const string QUEUE_NAME = "rpc_queue";

    private readonly IConnection connection;
    private readonly IModel channel;
    private readonly string replyQueueName;
    /// <summary>
    /// <see cref="ConcurrentDictionary{TKey, TValue}"/> 表示一个可以被多个线程同时访问的键/值对的线程安全集合。这个集合类是一个线程安全的实现，建议在多个线程可能同时访问的元素时使用它。<see href="https://learn.microsoft.com/zh-cn/dotnet/api/system.collections.concurrent.concurrentdictionary-2?view=net-6.0">了解更多</see>
    /// <para><see cref="TaskCompletionSource{TResult}"/> 表示未绑定到委托的 <see cref="Task{TResult}"/> 的制造者方，并通过 <see cref="TaskCompletionSource{TResult}.Task"/> 属性提供对使用者方的访问。该类通常用于将外部异步操作表示为 <see cref="Task{TResult}"/>。<see href="https://learn.microsoft.com/zh-cn/dotnet/api/system.threading.tasks.taskcompletionsource?view=net-6.0">了解更多</see></para>
    /// </summary>
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> callbackMapper = new();

    public RpcClient()
    {
        #region 创建 RabbitMQ 工厂实例；建立连接、通道
        var factory = new ConnectionFactory { HostName = "localhost" };

        connection = factory.CreateConnection();
        channel = connection.CreateModel();
        #endregion
        //声明一个 RabbitMQ 服务器自动命名的“回调”队列，默认为独占（排他）队列，获取队列名称
        replyQueueName = channel.QueueDeclare().QueueName;
        //创建（将 IBasicConsumer 接口实现为事件的）EventingBasicConsumer 类对象并关联指定的通道
        var consumer = new EventingBasicConsumer(channel);
        //使用 Lambda 表达式注册事件处理程序并订阅 Received 事件
        consumer.Received += (model, ea) =>
        {
            //ConcurrentDictionary<TKey, TValue>.TryRemove() 方法尝试从 ConcurrentDictionary<TKey, TValue> 中移除并返回具有指定键的值
            if (!callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out var tcs))
                return;
            var body = ea.Body.ToArray();
            var response = Encoding.UTF8.GetString(body);
            //尝试将底层 Task<TResult> 转换为 TaskStatus.RanToCompletion 状态。该方法接受一个参数 result，用于指定要绑定到此 Task<TResult> 的结果值
            tcs.TrySetResult(response);
        };

        //启动一个基本的内容类消费者（在当前通道中监听“回调”消息队列，并进行消费）
        channel.BasicConsume(consumer: consumer,        //指定用于接收消息的消费者对象
                             queue: replyQueueName,     //消息队列名称
                             autoAck: true);            //是否自动发送确认消息（acknowledgement）给 RabbitMQ 服务器
    }

    /// <summary>
    /// 该方法发送 RPC 请求并在收到结果前保持阻塞
    /// </summary>
    /// <remarks><see cref="Task{TResult}"/> 类代表可以返回一个值的单个异步操作。<see href="https://learn.microsoft.com/zh-cn/dotnet/api/system.threading.tasks.task-1?view=net-6.0">了解更多</see></remarks>
    /// <param name="message">请求消息字符串</param>
    /// <param name="cancellationToken">取消操作通知结构体。<see href="https://learn.microsoft.com/zh-cn/dotnet/api/system.threading.cancellationtoken?view=net-6.0">了解更多</see></param>
    /// <returns></returns>
    public Task<string> CallAsync(string message, CancellationToken cancellationToken = default)
    {
        #region 设置请求消息的 CorrelationId 和 ReplyTo
        IBasicProperties props = channel.CreateBasicProperties();
        var correlationId = Guid.NewGuid().ToString();              //使用 Guid（全局唯一标识符）作为请求消息的 CorrelationId
        props.CorrelationId = correlationId;                        //设置 CorrelationId
        props.ReplyTo = replyQueueName;                             //设置 ReplyTo
        #endregion
        var messageBytes = Encoding.UTF8.GetBytes(message);         //UTF-8 格式编码请求消息字节数组
        var tcs = new TaskCompletionSource<string>();               //TaskCompletionSource 类通常用于将外部异步操作表示为 Task<TResult>
        callbackMapper.TryAdd(correlationId, tcs);                  //尝试添加键/值对

        //发布 RPC 请求
        channel.BasicPublish(exchange: string.Empty,                //指定要将消息发布到哪个交换机（exchange），设置为空字符串以使用默认交换机
                             routingKey: QUEUE_NAME,                //指定消息的路由键（routing key），路由键用于确定消息应该被发送到哪些消息队列（发布到 rpc_queue）
                             basicProperties: props,                //指定请求消息的 CorrelationId 和 ReplyTo 属性
                             body: messageBytes);                   //消息内容，将要发送的数据转换为字节数组，并传递给这个参数

        /**
         * 在 C# 中，下划线（_）用作弃元（discard）的变量名。弃元是一种占位符变量，用于忽略不需要的值。
         * 例如：在调用具有 out 参数的方法时，如果您不需要该 out 参数返回的值，则可以使用弃元来忽略它。
         */
        cancellationToken.Register(() => callbackMapper.TryRemove(correlationId, out _));       //注册取消操作为尝试从 callbackMapper 中移除键值对
        return tcs.Task;                                            //返回异步操作对象
    }

    public void Dispose()
    {
        channel.Close();
        connection.Close();
    }
}

public class Rpc
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("RPC Client");
        await InvokeAsync();

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }

    /// <summary>
    /// 异步发送 RPC 请求方法
    /// </summary>
    /// <remarks><see langword="async"/> 和 <see langword="await"/> 关键字的使用请参考 <see href="https://learn.microsoft.com/zh-cn/dotnet/csharp/asynchronous-programming/task-asynchronous-programming-model">C# 异步编程模型</see></remarks>
    /// <returns>异步操作对象</returns>
    private static async Task InvokeAsync()
    {
        using var rpcClient = new RpcClient();

        Console.WriteLine("请输入您想查询的斐波那契数列索引：\r\n  * 请输入小于47的非负整数（最好在30以内），过大的数据会导致过长的响应时间\r\n  * 并且第46位斐波那契数（1836311903）已经接近 int 类型所能表达的数值（-2147483648~2147483647）极限，第47位就溢出了");
        string? n = Console.ReadLine();
        if(!string.IsNullOrEmpty(n))        //readline() 返回值不为 null 时执行相关操作
        {
            Console.WriteLine(" [x] 请求 fib({0})", n);
            var response = await rpcClient.CallAsync(n);
            Console.WriteLine(" [.] 第{0}位斐波那契数为： '{1}'", n, response);
        }
    }
}