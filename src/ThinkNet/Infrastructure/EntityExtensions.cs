using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// <see cref="IEntity"/> 的扩展类。
    /// </summary>
    public static class EntityExtensions
    {
        /// <summary>
        /// 验证模型的正确性
        /// </summary>
        public static bool IsValid(this IEntity entity, out IEnumerable<ModelValidationResult> errors)
        {
            errors = from property in TypeDescriptor.GetProperties(entity).Cast<PropertyDescriptor>()
                     from attribute in property.Attributes.OfType<ValidationAttribute>()
                     where !attribute.IsValid(property.GetValue(entity))
                     select new ModelValidationResult {
                         MemberName = property.Name,
                         Message = attribute.FormatErrorMessage(property.DisplayName ?? property.Name)
                     };

            return errors != null && errors.Any();
        }
    }
}
