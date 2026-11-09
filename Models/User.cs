using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace LearnMVC.Models;

public partial class User
{
    [Key]
    [Column("ID")]
    [StringLength(50)]
    public Guid ID { get; set; }

    [StringLength(50)]
    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }  
    [StringLength(50)]
    public string FullName {get;set;} = null!;
    public DateTime CreateAt {get;set;}

    [InverseProperty("User")]
    public virtual ICollection<FinanceSetting> FinanceSettings { get; set; } = new List<FinanceSetting>();

    [InverseProperty("User")]
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
