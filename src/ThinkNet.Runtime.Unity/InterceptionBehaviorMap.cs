using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.Unity.InterceptionExtension;


namespace ThinkNet.Configurations
{
    public sealed class InterceptionBehaviorMap
    {
        public static readonly InterceptionBehaviorMap Instance;
        static InterceptionBehaviorMap()
        {
            Instance = new InterceptionBehaviorMap();
        }


        private readonly IDictionary<Type, int[]> _behaviorMap;
        private readonly IDictionary<int, Type> _codeTypeDict;

        private InterceptionBehaviorMap()
        {
            this._behaviorMap = new Dictionary<Type, int[]>();
            this._codeTypeDict = new Dictionary<int, Type>();
        }

        public InterceptionBehaviorMap Mapping(Type serviceType, params Type[] behaviorTypes)
        {
            serviceType.NotNull("serviceType");
            serviceType.NotNull("behaviorType");

            if (behaviorTypes.Length <= 0) {
                //throw new ArgumentException("长度必须大于零。", "behaviorTypes");
                return this;
            }

            behaviorTypes.ForEach(behaviorType => {
                if (!typeof(IInterceptionBehavior).IsAssignableFrom(behaviorType)) {
                    throw new ArgumentException(string.Format("Type {0} no inheritance IInterceptionBehavior.", behaviorType.FullName));
                }
            });

            int length = behaviorTypes.Length;
            int[] typeCodes = new int[length];
            int index = 0;
            while (index < length) {
                typeCodes[index] = RegisterType(behaviorTypes[index++]);
            }

            _behaviorMap.Add(serviceType, typeCodes);

            return this;
        }

        private int RegisterType(Type type)
        {
            int code = type.FullName.GetHashCode();
            if (!_codeTypeDict.ContainsKey(code)) {
                _codeTypeDict.Add(code, type);
            }

            return code;
        }

        public Type[] GetBehaviorTypes(Type type)
        {
            int[] behaviorTypeCodes;
            if (!_behaviorMap.TryGetValue(type, out behaviorTypeCodes)) {
                return Type.EmptyTypes;
            }
            return behaviorTypeCodes.Select(code => _codeTypeDict[code]).ToArray();
        }
    }
}
