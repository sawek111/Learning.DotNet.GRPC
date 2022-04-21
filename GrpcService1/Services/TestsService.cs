using System;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;

namespace GrpcService1.Services
{
    public class TestsService : TestService.TestServiceBase
    {
        public override async Task<MessageReply> UnaryCall(MessageRequest request, ServerCallContext context)
        {
            await Task.Delay(1000);
            var returnMessage = $"You sent me: {request.MyMessage}";
            return new MessageReply { ResponseMessage = returnMessage };
        }

        public override async Task ServerSideStream(MessageRequest request,
            IServerStreamWriter<MessageReply> responseStream, ServerCallContext context)
        {
            for (var i = 0; i < 10; i++)
            {
                // if (context.CancellationToken.IsCancellationRequested)
                // {
                //     return;
                // }

                await responseStream.WriteAsync(new MessageReply()
                {
                    ResponseMessage = $"Your message is : {request.MyMessage}., it is iteration number: {i}"
                });
                await Task.Delay(1000);
            }
        }

        public override async Task<MessageReply> ClientSideStream(IAsyncStreamReader<MessageRequest> requestStream,
            ServerCallContext context)
        {
            var messageBuilder = new StringBuilder();
            await foreach (var request in requestStream.ReadAllAsync())
            {
                messageBuilder.AppendLine(request.MyMessage);
            }


            return new MessageReply { ResponseMessage = messageBuilder.ToString() };
        }

        public override async Task FullDuplex(IAsyncStreamReader<MessageRequest> requestStream,
            IServerStreamWriter<MessageReply> responseStream, ServerCallContext context)
        {
            var serverToClientCommunicationTask = ServerToClientStreamAsync(responseStream, context);
            var clientToServerCommunicationTask = ClientToServerStreamAsync(requestStream, context);

            await Task.WhenAll(serverToClientCommunicationTask, clientToServerCommunicationTask);
        }

        private async Task ClientToServerStreamAsync(IAsyncStreamReader<MessageRequest> requestStream,
            ServerCallContext context)
        {
            while (await requestStream.MoveNext() && !context.CancellationToken.IsCancellationRequested)
            {
                var message = requestStream.Current;
                Console.WriteLine($"Received: {message.MyMessage}");
            }
        }

        private static async Task ServerToClientStreamAsync(IServerStreamWriter<MessageReply> responseStream,
            ServerCallContext context)
        {
            var count = 0;
            while (!context.CancellationToken.IsCancellationRequested)
            {
                await responseStream.WriteAsync(new MessageReply
                {
                    ResponseMessage = $"Response from server number {++count}"
                });
                await Task.Delay(1000);
            }
        }
    }
}