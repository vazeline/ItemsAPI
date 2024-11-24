using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Common.Validation
{
    public class ValidEnumValueAttribute : ValidationAttribute
    {
        private readonly Type enumType;
        private readonly bool isRequired;
        private readonly int[] disallowedValues;

        public ValidEnumValueAttribute(Type enumType, bool isRequired)
            : this(enumType, isRequired, null)
        {
        }

        public ValidEnumValueAttribute(Type enumType, bool isRequired, int[] disallowedValues)
        {
            if (!enumType.IsEnum)
            {
                throw new ArgumentException("Type must be an enum.");
            }

            this.enumType = enumType;
            this.isRequired = isRequired;
            this.disallowedValues = disallowedValues;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
            {
                if (this.isRequired)
                {
                    return new ValidationResult($"A value for parameter '{validationContext.DisplayName}' is required.");
                }

                return ValidationResult.Success;
            }

            if (!Enum.IsDefined(this.enumType, value))
            {
                return new ValidationResult($"The value '{value}' is not a valid member of the enum {this.enumType.Name}.");
            }

            if (this.disallowedValues?.Contains((int)value) == true)
            {
                return new ValidationResult($"The value '{value}' is not permitted.");
            }

            return ValidationResult.Success;
        }
    }
}
