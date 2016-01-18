using System;

namespace UserRegistration.ReadModel
{
    public class UserModel
    {
        public Guid UserID { get; set; }

        public string LoginId { get; set; }

        public string Password { get; set; }

        public string UserName { get; set; }

        public override bool Equals(object obj)
        {
            var other  = obj as UserModel;
            if (other == null)
                return false;

            return this.UserID == other.UserID;
        }

        public override int GetHashCode()
        {
            return this.UserID.GetHashCode();
        }
    }
}
