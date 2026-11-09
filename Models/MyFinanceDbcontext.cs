using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace LearnMVC.Models;

public partial class MyFinanceDbcontext : DbContext
{
    public MyFinanceDbcontext()
    {
    }

    public MyFinanceDbcontext(DbContextOptions<MyFinanceDbcontext> options)
        : base(options)
    {
    }

    public virtual DbSet<FinanceSetting> FinanceSettings { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<User> Users { get; set; }
}

