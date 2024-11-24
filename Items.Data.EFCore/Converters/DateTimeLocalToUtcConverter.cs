using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Items.Data.EFCore.Converters
{
    public class DateTimeLocalToUtcConverter : ValueConverter<DateTime, DateTime>
    {
        public DateTimeLocalToUtcConverter()
            : base(
                convertToProviderExpression: utcDateTime => utcDateTime.ToLocalTime(),
                convertFromProviderExpression: localDateTime => localDateTime.ToUniversalTime())
        {
        }
    }
}
