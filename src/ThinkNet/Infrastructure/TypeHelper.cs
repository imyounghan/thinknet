using System;
using System.Linq;
using ThinkNet.Kernel;
using ThinkNet.Messaging;


namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// A utility class provides type related methods.
    /// </summary>
    public static class TypeHelper
    {
        /// <summary>Check whether a type is an aggregate root type.
        /// </summary>
        public static bool IsAggregateRoot(Type type)
        {
            return type.IsClass && !type.IsAbstract && typeof(IAggregateRoot).IsAssignableFrom(type);
        }

        /// <summary>
        /// Check whether a type is a eventsouced aggregate root type.
        /// </summary>
        public static bool IsEventSourced(Type type)
        {
            return type.IsClass && !type.IsAbstract && typeof(IEventSourced).IsAssignableFrom(type);
        }

        /// <summary>Check whether a type is a repository type.
        /// </summary>
        public static bool IsRepositoryInterfaceType(Type type)
        {
            Func<Type, bool> predicate = new Func<Type, bool>(target => {
                return target.IsInterface && target.IsGenericType && target.GetGenericTypeDefinition() == typeof(IRepository<>);
            });

            return predicate(type) || type.GetInterfaces().Any(predicate);
        }

        /// <summary>Check whether a type is a repository.
        /// </summary>
        public static bool IsRepositoryType(Type type)
        {
            return type.IsClass && !type.IsAbstract &&
                type.GetInterfaces().Any(IsRepositoryInterfaceType);
        }

        /// <summary>
        /// Check whether a type is a message type.
        /// </summary>
        public static bool IsMessage(Type type)
        {
            return type.IsClass && !type.IsAbstract && typeof(IMessage).IsAssignableFrom(type);
        }

        /// <summary>
        /// Check whether a type is a command type.
        /// </summary>
        public static bool IsCommand(Type type)
        {
            return type.IsClass && !type.IsAbstract && typeof(ICommand).IsAssignableFrom(type);
        }
        /// <summary>
        /// Check whether a type is a command handler type.
        /// </summary>
        public static bool IsCommandHandlerInterfaceType(Type type)
        {
            return type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICommandHandler<>);
        }
        /// <summary>
        /// Check whether a type is a command handler.
        /// </summary>
        public static bool IsCommandHandlerType(Type type)
        {
            return type.IsClass && !type.IsAbstract &&
                type.GetInterfaces().Any(IsCommandHandlerInterfaceType);
        }

        /// <summary>
        /// Check whether a type is a event type.
        /// </summary>
        public static bool IsEvent(Type type)
        {
            return type.IsClass && !type.IsAbstract && typeof(IEvent).IsAssignableFrom(type);
        }
        /// <summary>
        /// Check whether a type is a event handler type.
        /// </summary>
        public static bool IsEventHandlerInterfaceType(Type type)
        {
            return type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEventHandler<>);
        }
        /// <summary>
        /// Check whether a type is a event handler.
        /// </summary>
        public static bool IsEventHandlerType(Type type)
        {
            return type.IsClass && !type.IsAbstract &&
                type.GetInterfaces().Any(IsEventHandlerInterfaceType);
        }

        /// <summary>
        /// Check whether a type is a handler type.
        /// </summary>
        public static bool IsHandlerInterfaceType(Type type)
        {
            return type.IsInterface && typeof(IHandler).IsAssignableFrom(type);
        }
        /// <summary>
        /// Check whether a type is a handler.
        /// </summary>
        public static bool IsHandlerType(Type type)
        {
            return type.IsClass && !type.IsAbstract &&
                type.GetInterfaces().Any(IsHandlerInterfaceType);
        }

        /// <summary>
        /// Check whether a type is a handler type.
        /// </summary>
        public static bool IsMessageHandlerInterfaceType(Type type)
        {
            return type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IMessageHandler<>);
        }
        /// <summary>
        /// Check whether a type is a handler.
        /// </summary>
        public static bool IsMessageHandlerType(Type type)
        {
            return type.IsClass && !type.IsAbstract &&
                type.GetInterfaces().Any(IsMessageHandlerInterfaceType);
        }
    }
}
