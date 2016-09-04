using System;

namespace UserRegistration
{
    public class LoginNameData
    {
        public string LoginName { get; set; }

        public string CorrelationId { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as LoginNameData;
            if(obj == null) {
                return false;
            }

            return string.Equals(other.LoginName, this.LoginName, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return this.LoginName.ToLowerInvariant().GetHashCode();
        }
    }
}
