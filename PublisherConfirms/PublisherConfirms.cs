using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using RabbitMQ.Client;

const int MESSAGE_COUNT = 50_000;               //消息数量为 50,000

PublishMessagesIndividually();                  //策略 #1：单独发布消息
PublishMessagesInBatch();                       //策略 #2：批量发布消息
await HandlePublishConfirmsAsynchronously();    //策略 #3：异步处理发布者确认

//创建连接对象
static IConnection CreateConnection()
{
    var factory = new ConnectionFactory { HostName = "localhost" };
    return factory.CreateConnection();
}

//策略 #1：单独发布消息
static void PublishMessagesIndividually()
{
    using var connection = CreateConnection();
    using var channel = connection.CreateModel();           //创建一个新的通道

    //声明一个服务器命名队列
    var queueName = channel.QueueDeclare().QueueName;
    //在通道层级启用发布者确认
    channel.ConfirmSelect();

    //记录开始时间戳
    var startTime = Stopwatch.GetTimestamp();

    for (int i = 0; i < MESSAGE_COUNT; i++)
    {
        var body = Encoding.UTF8.GetBytes(i.ToString());    //UTF-8 编码得到字节数组
        channel.BasicPublish(exchange: string.Empty, routingKey: queueName, basicProperties: null, body: body);     //发布消息
        channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));      //超时时间为 5 秒情况下等待所有已发布消息确认
    }

    //记录结束时间戳
    var endTime = Stopwatch.GetTimestamp();

    Console.WriteLine($"在 {Stopwatch.GetElapsedTime(startTime, endTime).TotalMilliseconds:N0} ms 内单独发布 {MESSAGE_COUNT:N0} 条消息");
}

//策略 #2：批量发布消息
static void PublishMessagesInBatch()
{
    using var connection = CreateConnection();
    using var channel = connection.CreateModel();       //创建一个新的通道

    //声明一个服务器命名队列
    var queueName = channel.QueueDeclare().QueueName;
    //在通道层级启用发布者确认
    channel.ConfirmSelect();

    var batchSize = 100;                                //批处理大小（100 条消息为一批）
    var outstandingMessageCount = 0;                    //记录未确认的消息数

    //记录开始时间戳
    var startTime = Stopwatch.GetTimestamp();

    for (int i = 0; i < MESSAGE_COUNT; i++)
    {
        var body = Encoding.UTF8.GetBytes(i.ToString());    //UTF-8 编码得到字节数组
        channel.BasicPublish(exchange: string.Empty, routingKey: queueName, basicProperties: null, body: body);     //发布消息
        outstandingMessageCount++;                      //每发布一条消息，那么未确认消息数量加一

        if (outstandingMessageCount == batchSize)
        {
            channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));      //超时时间为 5 秒情况下等待所有已发布消息确认
            outstandingMessageCount = 0;                //未确认消息数归零
        }
    }

    //如果已发布（未确认）的消息数不满足一批（不到 batchSize 个），那么就当作一批处理
    if (outstandingMessageCount > 0)
        channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));          //超时时间为 5 秒情况下等待所有已发布消息确认

    //记录结束时间戳
    var endTime = Stopwatch.GetTimestamp();

    Console.WriteLine($"在 {Stopwatch.GetElapsedTime(startTime, endTime).TotalMilliseconds:N0} ms 内批量发布 {MESSAGE_COUNT:N0} 条消息");
}

//策略 #3：异步处理发布者确认
static async Task HandlePublishConfirmsAsynchronously()
{
    using var connection = CreateConnection();
    using var channel = connection.CreateModel();       //创建一个新的通道

    //声明一个服务器命名队列
    var queueName = channel.QueueDeclare().QueueName;
    //在通道层级启用发布者确认
    channel.ConfirmSelect();

    /// <remarks>使用字典关联序列号与消息</remarks>
    /// <see cref="ConcurrentDictionary{TKey, TValue}"/> 表示一个可以被多个线程同时访问的键/值对的线程安全集合。
    /// 这个集合类是一个线程安全的实现，建议在多个线程可能同时访问的元素时使用它。
    /// <see href="https://learn.microsoft.com/zh-cn/dotnet/api/system.collections.concurrent.concurrentdictionary-2?view=net-6.0">了解更多</see>
    var outstandingConfirms = new ConcurrentDictionary<ulong, string>();

    //确认达到时清理字典（包括单次和多次确认）
    void CleanOutstandingConfirms(ulong sequenceNumber, bool multiple)
    {
        if (multiple)       //多次确认
        {
            var confirmed = outstandingConfirms.Where(k => k.Key <= sequenceNumber);        //所有序列号小于等于该序列号的字典条目都清理掉
            foreach (var entry in confirmed)
                outstandingConfirms.TryRemove(entry.Key, out _);
        }
        else                //单次确认
            outstandingConfirms.TryRemove(sequenceNumber, out _);
    }

    channel.BasicAcks += (sender, ea) => CleanOutstandingConfirms(ea.DeliveryTag, ea.Multiple);
    channel.BasicNacks += (sender, ea) =>
    {
        outstandingConfirms.TryGetValue(ea.DeliveryTag, out string? body);                  //检索 nack-ed 消息体
        Console.WriteLine($"消息体为 {body} 的消息已被拒绝（nack-ed）。序列号：{ea.DeliveryTag}，是否批量确认消息：{ea.Multiple}");
        CleanOutstandingConfirms(ea.DeliveryTag, ea.Multiple);                              //即便消息已 nack，也必须删除字典中对应的条目
    };

    //记录开始时间戳
    var startTime = Stopwatch.GetTimestamp();

    for (int i = 0; i < MESSAGE_COUNT; i++)
    {
        var body = i.ToString();
        outstandingConfirms.TryAdd(channel.NextPublishSeqNo, i.ToString());                 //在发布消息之前跟踪序列号
        channel.BasicPublish(exchange: string.Empty, routingKey: queueName, basicProperties: null, body: Encoding.UTF8.GetBytes(body));     //发布消息
    }

    //当字典不为空时，异步等待 60 秒，直至字典清空（不抛出异常）或者等待超时（抛出异常）
    if (!await WaitUntil(60, () => outstandingConfirms.IsEmpty))
        throw new Exception("在 60 秒内，所有的消息都未得到确认");

    //记录结束时间戳
    var endTime = Stopwatch.GetTimestamp();
    Console.WriteLine($"在 {Stopwatch.GetElapsedTime(startTime, endTime).TotalMilliseconds:N0} ms 内发布 {MESSAGE_COUNT:N0} 条消息并异步处理确认");
}


/// <summary>
/// 直到 <paramref name="numberOfSeconds"/> 秒前保持等待
/// <remarks><see langword="async"/> 和 <see langword="await"/> 关键字的使用请参考 <see href="https://learn.microsoft.com/zh-cn/dotnet/csharp/asynchronous-programming/task-asynchronous-programming-model">C# 异步编程模型</see></remarks>
/// </summary>
/// <param name="numberOfSeconds">秒数</param>
/// <param name="condition">某种情况，<see cref="System.Func{TResult}"/> 封装了一个方法，该方法没有参数并返回一个由 <typeparamref name="TResult"/> 参数指定类型的值。<paramref name="condition"/> 即表示该方法</param>
/// <return><see cref="ValueTask{TResult}"/> 提供一个值类型，它包装 <see cref="Task{TResult}"/> 和 <typeparamref name="TResult"/>，只使用其中一个</return>
static async ValueTask<bool> WaitUntil(int numberOfSeconds, Func<bool> condition)
{
    int waited = 0;
    while (!condition() && waited < numberOfSeconds * 1000)     //调用 condition 封装的方法，当方法返回 false 且等待的时间小于 numberOfSeconds 时继续异步等待
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        waited += 100;
    }

    return condition();                                         //等待超时（condition() 返回 false）或者 condition 发生变化（condition() 返回 true）时退出
}