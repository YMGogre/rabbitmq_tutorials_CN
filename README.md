# RabbitMQ .NET C# 代码 教程

在这儿您可以找到使用 .NET 7.0 的 [RabbitMQ
教程](https://www.rabbitmq.com/getstarted.html)的 C# 代码示例。

您还可以找到 Visual Studio 2022 的解决方案文件。

要成功使用这些示例，您需要一个正在运行的 RabbitMQ 服务器。

## 要求

### Windows 要求

* [dotnet core](https://www.microsoft.com/net/core)

我们使用命令行 (start->run cmd.exe) 来编译并运行代码。 或者，您可以使用 Visual Studio，但本教程假定使用命令行。

### Linux 要求

* [dotnet core](https://www.microsoft.com/net/core)

### Code

每个命令最好在 从教程目录的根目录运行的单独的控制台/终端实例 中运行。
![指定目录启动 cmd](./image/Q%26A/3-%E5%9C%A8%E7%89%B9%E5%AE%9A%E7%9B%AE%E5%BD%95%E4%B8%8B%E5%90%AF%E5%8A%A8%E5%91%BD%E4%BB%A4%E6%8F%90%E7%A4%BA%E7%AC%A6.gif)

#### [教程一: RabbitMQ 入门 以及 "Hello World!"](https://blog.csdn.net/YMGogre/article/details/131087813)

```shell
dotnet run --project RabbitMQProducer/RabbitMQProducer.csproj
dotnet run --project RabbitMQConsumer/RabbitMQConsumer.csproj
```

#### [教程二：工作队列](https://blog.csdn.net/YMGogre/article/details/131561085)

```shell
dotnet run --project Worker/Worker.csproj
dotnet run --project NewTask/NewTask.csproj
```

#### [教程三：发布/订阅](https://blog.csdn.net/YMGogre/article/details/131568234)

```shell
dotnet run --project ReceiveLogs/ReceiveLogs.csproj
dotnet run --project EmitLog/EmitLog.csproj
```

#### [教程四：路由](https://blog.csdn.net/YMGogre/article/details/131766884)

```shell
dotnet run --project ReceiveLogsDirect/ReceiveLogsDirect.csproj info
dotnet run --project EmitLogDirect/EmitLogDirect.csproj
```

#### [教程五：主题](https://blog.csdn.net/YMGogre/article/details/131774981)

```shell
dotnet run --project ReceiveLogsTopic/ReceiveLogsTopic.csproj anonymous.info
dotnet run --project EmitLogTopic/EmitLogTopic.csproj
```

#### [教程六: RPC](https://blog.csdn.net/YMGogre/article/details/131472742)

```shell
dotnet run --project RPCServer/RPCServer.csproj
dotnet run --project RPCClient/RPCClient.csproj
```

#### [教程七：发布者确认](https://blog.csdn.net/YMGogre/article/details/131804368)

```shell
dotnet run --project PublisherConfirms/PublisherConfirms.csproj
```