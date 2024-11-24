using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Items.Data.EFCore.Abstraction.Interfaces
{
    public interface IDatabaseSeeder
    {
        void Seed(DbContext context);
    }
}
