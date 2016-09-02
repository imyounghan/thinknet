using System;

namespace UserRegistration.ReadModel
{
    public class UserModel
    {
        public UserModel()
        { }

        public UserModel(string loginId)
        {
            this.LoginId = loginId;
        }

        public Guid UserID { get; set; }

        public string LoginId { get; set; }

        public string Password { get; set; }

        public string UserName { get; set; }

        public override bool Equals(object obj)
        {
            var other  = obj as UserModel;
            if (other == null)
                return false;

            return this.LoginId == other.LoginId;
        }

        public override int GetHashCode()
        {
            return this.LoginId.GetHashCode();
        }
    }
}
