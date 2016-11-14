using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ThinkNet.Common.Interception
{
    public class ParameterCollection : IParameterCollection
    {
        struct ArgumentInfo
        {
            public int Index;
            public string Name;
            public ParameterInfo ParameterInfo;

            public ArgumentInfo(int index, ParameterInfo parameter)
            {
                this.Index = index;
                this.Name = parameter.Name;
                this.ParameterInfo = parameter;
            }
        }

        private readonly List<ArgumentInfo> parameters;
        private readonly object[] arguments;

        public ParameterCollection(object[] arguments, ParameterInfo[] parameters, Predicate<ParameterInfo> isArgumentPartOfCollection)
        {
            arguments.NotNull("arguments");
            parameters.NotNull("parameters");

            this.arguments = arguments;
            this.parameters = new List<ArgumentInfo>();
            
            for(int index = 0; index < parameters.Length; ++index) {
                if(isArgumentPartOfCollection(parameters[index])) {
                    this.parameters.Add(new ArgumentInfo(index, parameters[index]));
                }
            }
        }

        private int IndexForInputParameterName(string parameterName)
        {
            var index = parameters.FindIndex(p => p.Name == parameterName);
            if(index == -1)
                throw new ArgumentException("Invalid parameter Name", "paramName");

            return index;
        }

        #region IParameterCollection 成员

        public object this[string parameterName]
        {
            get
            {
                return arguments[parameters[IndexForInputParameterName(parameterName)].Index];
            }
        }

        public bool ContainsParameter(string parameterName)
        {
            return parameters.Any(p => p.Name == parameterName);
        }

        public ParameterInfo GetParameterInfo(int index)
        {
            return parameters[index].ParameterInfo;
        }

        public ParameterInfo GetParameterInfo(string parameterName)
        {
            return this.GetParameterInfo(IndexForInputParameterName(parameterName));
        }

        #endregion        

        #region ICollection 成员

        public void CopyTo(Array array, int index)
        {
            int destIndex = 0;
            parameters.GetRange(index, parameters.Count - index).ForEach(
                delegate (ArgumentInfo info) {
                    array.SetValue(arguments[info.Index], destIndex++);
                });
        }

        public int Count
        {
            get { return parameters.Count; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return this; }
        }

        #endregion

        #region IEnumerable 成员

        public IEnumerator GetEnumerator()
        {
            for(int i = 0; i < parameters.Count; ++i) {
                yield return arguments[parameters[i].Index];
            }
        }

        #endregion
    }
}
