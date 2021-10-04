using Infrastructure.EventSourcing;
using Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Infrastructure.Messaging.Handling
{
    public class CommandAndQueryListProvider : ICommandAndQueryListDao
    {
        private static CommandAndQueryListProvider _instance = null;

        private readonly Dictionary<RequestTypeDescriptor, HandlerTypeDescriptor> handlersByMessage = new Dictionary<RequestTypeDescriptor, HandlerTypeDescriptor>();

        private CommandAndQueryListProvider()
        {
        }

        public static CommandAndQueryListProvider GetInstance()
            => _instance ?? new CommandAndQueryListProvider();

        public void Register(ICommandHandler handler)
        {
            var genericHandler = typeof(ICommandHandler<>);
            var genericResponseHandler = typeof(ICommandHandler<,>);
            var supportedCommandTypes = handler.GetType()
                .GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition().Transform(d => d == genericHandler || d == genericResponseHandler))
                .Select(i => this.ParseMessage(i.GetGenericArguments()[0]))
                .ToList();

            if (this.handlersByMessage.Keys.Any(registeredType => supportedCommandTypes.Contains(registeredType)))
                throw new ArgumentException("The command handled by the received handler has already a registered handler.");

            var handlerDesc = ParseHandler(handler);
            supportedCommandTypes.ForEach(commandType => this.handlersByMessage.Add(commandType, handlerDesc));
        }

        public void Register(IQueryHandler handler)
        {
            var genericHandler = typeof(IQueryHandler<>);
            // For new type of query handler
            var queryResponseHandler = typeof(IQueryHandler<,>);
            var supportedQueryTypes = handler.GetType()
                .GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition().Transform(d => d == genericHandler || d == queryResponseHandler))
                .Select(i => this.ParseMessage(i.GetGenericArguments()[0]))
                .ToList();

            if (this.handlersByMessage.Keys.Any(registeredType => supportedQueryTypes.Contains(registeredType)))
                throw new ArgumentException("The query handled by the received handler has already a registered handler.");

            var handlerDesc = ParseHandler(handler);
            supportedQueryTypes.ForEach(queryType => this.handlersByMessage.Add(queryType, handlerDesc));
        }

        #region Dao implementation

        public string ParseRequestToken(IMessage request)
            => this.ParseRequestToken(request.GetType());

        public string ParseRequestToken(Type requestType) 
            => requestType.Name;

        public string ParseRequestToken<T>() where T : IMessage
            => this.ParseRequestToken(typeof(T));

        public ICollection<string> RequestTokens => this.handlersByMessage.Keys.Select(k => k.Token).ToList();

        public ICollection<RequestGroupDto> RequestsByGroup =>
            this.handlersByMessage
                .Aggregate(new List<RequestGroupDto>(),
                    (list, handlerByMsg) =>
                    {
                        var msg = handlerByMsg.Key;
                        var handler = handlerByMsg.Value;

                        var group = list.SingleOrDefault(x => x.Name == handler.Description && x.IsQuery == handler.IsQueryHandler);
                        if (group is null)
                        {
                            group = new RequestGroupDto
                            {
                                Name = handler.Description,
                                IsQuery = handler.IsQueryHandler
                            };
                            list.Add(group);
                        }

                        group.Requests.Add(new RequestDto
                        {
                            Token = msg.Token,
                            Description = msg.Description
                        });

                        return list;
                    });

        #endregion

        private RequestTypeDescriptor ParseMessage(Type message)
        {
            // Todo: find description in decorator if applicable 
            var token = this.ParseRequestToken(message);
            return new RequestTypeDescriptor(token, TransformCamelCaseToText(token));
        }

        private static HandlerTypeDescriptor ParseHandler(ICommandHandler handler)
        {
            // Todo: find description in decorator if applicable
            var description = TransformCamelCaseToText(
                                handler.GetType().Name);

            return new HandlerTypeDescriptor(description, false);
        }

        private static HandlerTypeDescriptor ParseHandler(IQueryHandler handler)
        {
            // Todo: find description in decorator if applicable
            var description = TransformCamelCaseToText(handler.GetType().Name);

            return new HandlerTypeDescriptor(description, true);
        }

        private static string TransformCamelCaseToText(string camelCase)
        {
            // Thanks to magma https://stackoverflow.com/questions/5796383/insert-spaces-between-words-on-a-camel-cased-token
            var text = Regex.Replace(camelCase, "(\\B[A-Z])", " $1");
            return text.ToLower().WithFirstCharInUpper();
        }
    }


    internal class RequestTypeDescriptor : ValueObject<RequestTypeDescriptor>
    {
        public RequestTypeDescriptor(string token, string description)
        {
            this.Token = token;
            this.Description = description;
        }

        public string Token { get; }
        public string Description { get; }
    }

    internal class HandlerTypeDescriptor : ValueObject<HandlerTypeDescriptor>
    {
        public HandlerTypeDescriptor(string description, bool isQueryHandler)
        {
            this.Description = description;
            this.IsQueryHandler = isQueryHandler;
        }

        public string Description { get; }
        public bool IsQueryHandler { get; }
    }
}
